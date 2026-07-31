using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Ported from `legacy-reference/DAL/MHNHL7/DBMessages.cs`'s `ExecuteHisoProcedure`/`GetParamList`/
/// `MapParamList` (HISO-BR-03's database-driven concept-mapping engine). Not "dormant" - this is
/// live, foundational plumbing most HISO-sourced Block 2 read endpoints call through.
///
/// Deviations from the legacy source, all flagged:
/// - Connection resolution goes through <see cref="IHisoPracticeConnectionResolver"/> (HekTenantRegistry.HisoSessions,
///   2026-07-22 correction - never Practices) (ADR-001
///   tenant registry) instead of a fixed `ConectionStringPMS_NZ` app setting.
/// - The legacy "second database node" routing for specific report/attachment/letter/problem
///   procedures (HISO-BR-05) IS implemented (v1.1 Step 7, 2026-07-24) via <see cref="ILegacyPracticeConnectionResolver.ResolveSecondNodeAsync"/>,
///   confirmed global-per-environment (not per-practice) from the real legacy source. Deliberate
///   deviation: legacy's own `strConnection`/`strConnectionSecondNode` module statics leave
///   parameter-list discovery on whichever connection was last set (a real latent bug in the legacy
///   source, not intentional behavior) - this port always uses the primary connection for parameter
///   discovery and only the actual data-returning call(s) use the second node.
/// - The AWS-enabled branch (`ExecuteAWSFlow`/`EnrichWithAWS`) is ported but throws
///   <see cref="NotSupportedException"/> if actually reached - `AWSDoc.IndiciDMS`'s source was
///   never available even to the original Phase 1 analysis (SRS Section 16), so there is nothing
///   to port it against. Falls back to the non-AWS procedure path is NOT attempted automatically,
///   since the legacy fallback behavior itself depends on AWS-check logic this repo doesn't have
///   confirmed detail for - flagged rather than guessed.
/// - `Logger.Logging.Instance` calls replaced with `ILogger&lt;T&gt;` (constructor-injected, matches
///   the rest of this codebase - never a static singleton logger).
/// </summary>
public sealed class HisoConceptExecutor : IHisoConceptExecutor
{
    private static readonly HashSet<string> AwsEnabledProcedures = new(StringComparer.OrdinalIgnoreCase)
    {
        "Hiso.uspGetPatient_Attachment",
        "Hiso.uspGetPatient_IncomingLetter",
        "Hiso.uspGetPatient_OutgoingLetter",
        "Hiso.uspGetPatient_OutgoingLetter_Author",
        "Hiso.uspGetPatient_LaboratoryReport",
        "Hiso.uspGetPatient_RadiologyReport"
    };

    private readonly IHisoPracticeConnectionResolver _connectionResolver;
    private readonly ILegacyPracticeConnectionResolver _legacyConnectionResolver;
    private readonly IHisoDmsConnectionResolver _dmsConnectionResolver;
    private readonly IAwsDocumentService _awsDocumentService;
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<HisoConceptExecutor> _logger;

    public HisoConceptExecutor(IHisoPracticeConnectionResolver connectionResolver, ILegacyPracticeConnectionResolver legacyConnectionResolver, IHisoDmsConnectionResolver dmsConnectionResolver, IAwsDocumentService awsDocumentService, ISecretProvider secretProvider, ILogger<HisoConceptExecutor> logger)
    {
        _connectionResolver = connectionResolver;
        _legacyConnectionResolver = legacyConnectionResolver;
        _dmsConnectionResolver = dmsConnectionResolver;
        _awsDocumentService = awsDocumentService;
        _secretProvider = secretProvider;
        _logger = logger;
    }

    public async Task<DataSet?> ExecuteAsync(string procedureName, HealthLinkSession session, IReadOnlyList<HisoRequest> requests, CancellationToken ct = default)
    {
        _logger.LogInformation("[ExecuteHisoProcedure] Started | Procedure: {ProcedureName} | PracticeId: {PracticeId}", procedureName, session.PracticeId);

        try
        {
            var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);

            // v1.1 spec follow-through, Step 7 (2026-07-24): real HISO-BR-05 second-node routing,
            // confirmed from legacy-reference/Hiso/DAL/DBMessages.cs's `ExecuteHisoProcedure` - these
            // 6 specific procedures' actual data query runs against a second, global-per-environment
            // connection, not the practice's own registered one. Deliberate deviation from the legacy
            // source: legacy also (inconsistently) leaves parameter-list discovery on whatever
            // module-static `strConnection` last held, which only reliably holds the primary connection
            // - reproducing that exactly would mean copying a real latent bug, not a feature, so
            // parameter discovery here always uses the primary connection and only the actual
            // data-returning call(s) below use the second node.
            var dataConnectionString = HisoSecondNodeProcedures.RequiresSecondNode(procedureName)
                ? await _legacyConnectionResolver.ResolveSecondNodeAsync(ct)
                : connectionString;

            var paramList = await GetParamListAsync(procedureName, session, connectionString, ct);
            var sqlParams = MapToSqlParameters(paramList, requests, procedureName);

            if (AwsEnabledProcedures.Contains(procedureName) && int.TryParse(session.PracticeId, out var practiceIdInt))
            {
                var awsEnabled = await _awsDocumentService.CheckAwsIsEnabledAsync(practiceIdInt, connectionString, ct);
                _logger.LogInformation(
                    "[ExecuteHisoProcedure] {ProcedureName} is AWS-enrichment-eligible; real CheckAWSIsEnabled returned {AwsEnabled} for practice {PracticeId}.",
                    procedureName, awsEnabled, practiceIdInt);

                if (awsEnabled)
                {
                    // v1.1 spec follow-through (2026-07-24): real AWS flow, ported from
                    // legacy-reference/Hiso/DAL/DBMessages.cs's ExecuteAWSFlow/EnrichWithAWS, now that
                    // the real AWSDocCore.dll is wired (Step 5) and the real "_AWS"-suffixed procedure
                    // is confirmed to exist (verified live via sqlcmd before writing this). Legacy calls
                    // a genuinely different stored procedure, not the plain one with extra params.
                    var awsProcedureName = procedureName + "_AWS";
                    var referenceId = string.Join(",", requests.Where(r => r.ProcedureName == procedureName && !string.IsNullOrEmpty(r.GroupreferenceID)).Select(r => r.GroupreferenceID));

                    try
                    {
                        var awsResult = sqlParams.Count > 0
                            ? await LegacyDbExecutor.ExecuteDataSetAsync(dataConnectionString, CommandType.StoredProcedure, awsProcedureName, CloneParams(sqlParams), ct)
                            : await LegacyDbExecutor.ExecuteDataSetAsync(dataConnectionString, CommandType.StoredProcedure, awsProcedureName, null, ct);

                        if (awsResult?.Tables.Count > 0 && awsResult.Tables[0].Rows.Count > 0)
                        {
                            // Real DMS content lives in a separate DMS_PMS database (confirmed from
                            // the real concept dictionary: Patient_Attachment_Content maps to
                            // DMS_PMS.dbo.tblDocumentDetail) - not the PMS_NZ_V2 connection used for
                            // everything else. Routed per-practice via `IHisoDmsConnectionResolver`
                            // (2026-07-30) - same server/credentials as the practice's primary
                            // connection, only the database name differs, matching the pattern already
                            // established for ERMS/KARO. Replaces the earlier single global
                            // `Hiso:DmsConnectionString` secret, which silently routed every practice to
                            // the same DMS database regardless of whose session was actually being served.
                            var dmsConnectionString = await _dmsConnectionResolver.ResolveAsync(session.PracticeId, ct);

                            // The `_AWS` procedure only ever populates Content for documents actually
                            // stored in S3 (`EnrichWithAwsAsync` below downloads those) - for a document
                            // stored locally in the DMS (not AWS), the `_AWS` procedure's Content column
                            // comes back genuinely blank by design, and there is no automatic fallback to
                            // the plain procedure's own inline content. Confirmed live (2026-07-30): the
                            // real reference API returns real base64 content for exactly this case, so
                            // the plain procedure's result is fetched here too and used to backfill
                            // Content/Size/Filename/DataType for any row the AWS flow left empty.
                            var plainResult = sqlParams.Count > 0
                                ? await LegacyDbExecutor.ExecuteDataSetAsync(dataConnectionString, CommandType.StoredProcedure, procedureName, CloneParams(sqlParams), ct)
                                : await LegacyDbExecutor.ExecuteDataSetAsync(dataConnectionString, CommandType.StoredProcedure, procedureName, null, ct);

                            await EnrichWithAwsAsync(awsResult.Tables[0], practiceIdInt, referenceId, dmsConnectionString, dataConnectionString, ct, plainResult?.Tables.Count > 0 ? plainResult.Tables[0] : null);
                            _logger.LogInformation("[ExecuteHisoProcedure] Completed (AWS flow) | Procedure={ProcedureName}", awsProcedureName);
                            return awsResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Legacy: AWS flow failure falls back to the plain procedure rather than
                        // failing the whole request - reproduced exactly.
                        _logger.LogWarning(ex, "{System} [ExecuteHisoProcedure] AWS flow failed for {ProcedureName}, falling back to the plain procedure.", "hiso", awsProcedureName);
                    }
                }
            }

            var result = sqlParams.Count > 0
                ? await LegacyDbExecutor.ExecuteDataSetAsync(dataConnectionString, CommandType.StoredProcedure, procedureName, sqlParams, ct)
                : await LegacyDbExecutor.ExecuteDataSetAsync(dataConnectionString, CommandType.StoredProcedure, procedureName, null, ct);

            _logger.LogInformation("[ExecuteHisoProcedure] Completed | Procedure={ProcedureName}", procedureName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{System} [ExecuteHisoProcedure] Failed executing {ProcedureName}", "hiso", procedureName);
            return null;
        }
    }

    /// <summary>
    /// Ported from `legacy-reference/Hiso/DAL/DBMessages.cs`'s `EnrichWithAWS`. Real column-derivation
    /// rule: find the `..._ID` column, strip `Patient_`/`_ID` to get the concept prefix (e.g.
    /// "Attachment"), then real content/size/filename/dataType/name/location/format columns are
    /// `Patient_{prefix}_*` - all already present (NULL) on the real `_AWS` procedure's result set
    /// (confirmed live via sqlcmd before writing this), matching legacy's `EnsureColumnExists` pattern.
    /// Real `AWSDocCore.dll` needs two connection strings (DMS + PMS_NZ) for
    /// `DocumentGetByDocumentKeyJsonResult`, unlike legacy's real 2-param call - confirmed from the
    /// real concept dictionary that `Patient_Attachment_Content` lives in a separate `DMS_PMS`
    /// database (per-practice, via `IHisoDmsConnectionResolver`), not the practice's main PMS_NZ
    /// connection.
    /// </summary>
    private async Task EnrichWithAwsAsync(DataTable table, int practiceId, string referenceId, string dmsConnectionString, string pmsConnectionString, CancellationToken ct, DataTable? plainFallbackTable = null)
    {
        var idColumn = table.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.EndsWith("_ID", StringComparison.OrdinalIgnoreCase))?.ColumnName;
        if (idColumn is null)
        {
            return;
        }

        var prefix = idColumn.Replace("Patient_", string.Empty).Replace("_ID", string.Empty);
        var contentCol = $"Patient_{prefix}_Content";
        var sizeCol = $"Patient_{prefix}_Size";
        var filenameCol = $"Patient_{prefix}_Filename";
        var dataTypeCol = $"Patient_{prefix}_DataType";

        if (!table.Columns.Contains(contentCol))
        {
            return;
        }

        // The `_AWS` procedure's result columns can come back ADO.NET-flagged ReadOnly, with a MaxLength
        // sized for the (empty/placeholder) values the plain SQL result normally carries - confirmed
        // live (2026-07-24) once real AWS bytes (tens of KB) started actually reaching this write, via
        // `System.Data.ReadOnlyException` then `MaxLength` `ArgumentException`.
        foreach (var col in new[] { contentCol, sizeCol, filenameCol, dataTypeCol })
        {
            if (table.Columns.Contains(col))
            {
                table.Columns[col]!.ReadOnly = false;
                table.Columns[col]!.MaxLength = -1;
            }
        }

        foreach (DataRow row in table.Rows)
        {
            if (row[idColumn] is DBNull)
            {
                continue;
            }

            var docKey = row[idColumn].ToString()?.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(docKey))
            {
                continue;
            }

            try
            {
                // Fetched first (rather than after the content-download attempt) because the real S3
                // download needs fields off this same status object - AWSTransactionID (the real
                // download identifier, not the document key) and the encrypted DMSAPI public/private
                // keys (confirmed via the real AWSDocCore source, 2026-07-24).
                var status = await _awsDocumentService.GetDocumentStatusFromIndiciAsync(docKey, practiceId, pmsConnectionString, ct);

                if (!string.IsNullOrEmpty(referenceId) && status is { IsAws: true })
                {
                    // Isolated in its own try/catch (rather than sharing the outer per-row one) so a
                    // failed/misconfigured S3 download (confirmed live 2026-07-30: `DocumentManager`'s
                    // base URL wasn't configured, throwing `ArgumentNullException` deep inside
                    // `AWSDocCore`) still falls through to the plain-procedure content fallback below,
                    // instead of aborting the rest of this row's enrichment entirely.
                    try
                    {
                        var bytes = await _awsDocumentService.DownloadFromAwsAsync(status, ct);
                        if (!string.IsNullOrEmpty(status.DocumentName) && bytes is { Length: > 0 })
                        {
                            // The `_AWS` procedure's Content column is declared varchar/nvarchar (a
                            // placeholder), not varbinary like the plain procedure's - assigning a raw
                            // byte[] into a string-typed DataColumn silently coerces via ToString()
                            // ("System.Byte[]" garbage), confirmed live (2026-07-24). Write whichever
                            // shape the column actually declares.
                            row[contentCol] = table.Columns[contentCol]!.DataType == typeof(byte[])
                                ? bytes
                                : Convert.ToBase64String(bytes);
                            row[sizeCol] = (long)bytes.Length;
                            row[filenameCol] = status.DocumentName;
                        }
                    }
                    catch (Exception downloadEx)
                    {
                        _logger.LogWarning(downloadEx, "{System} [EnrichWithAWS] S3 download failed for DocumentKey={DocumentKey}, falling back to plain-procedure content.", "hiso", docKey);
                    }
                }

                if (status is not null)
                {
                    if (string.IsNullOrEmpty(referenceId))
                    {
                        row[filenameCol] = status.DocumentName ?? (object)DBNull.Value;
                    }

                    if (status.DocumentType is not null)
                    {
                        row[dataTypeCol] = status.DocumentType;
                    }
                }

                // Not every document under an AWS-enabled practice is actually stored in S3 - a
                // locally-stored (DMS) document's `_AWS` row comes back with Content genuinely blank by
                // design (confirmed live, 2026-07-30: the plain procedure has the real inline content for
                // exactly such a document, while `_AWS` does not, and `status.IsAws` is false for it).
                // Legacy's real behavior for this case falls back to the plain procedure's own content
                // rather than leaving it empty - backfill from the matching plain-procedure row here.
                if (row[contentCol] is DBNull || (row[contentCol] is string s && string.IsNullOrEmpty(s)))
                {
                    var fallbackRow = plainFallbackTable?.Rows.Cast<DataRow>()
                        .FirstOrDefault(r => table.Columns.Contains(idColumn) && r.Table.Columns.Contains(idColumn)
                            && string.Equals(r[idColumn]?.ToString(), docKey, StringComparison.OrdinalIgnoreCase));

                    if (fallbackRow is not null)
                    {
                        foreach (var col in new[] { sizeCol, filenameCol, dataTypeCol })
                        {
                            if (table.Columns.Contains(col) && fallbackRow.Table.Columns.Contains(col) && fallbackRow[col] is not DBNull)
                            {
                                row[col] = fallbackRow[col];
                            }
                        }

                        // Same type mismatch as the AWS-download path above: the plain procedure's
                        // Content column is real `varbinary` (byte[]), the `_AWS` procedure's is a
                        // `varchar` placeholder - a direct assignment silently coerces via ToString()
                        // ("System.Byte[]" garbage) rather than throwing, so downstream base64 decoding
                        // fails with a confusing FormatException (confirmed live 2026-07-30).
                        if (table.Columns.Contains(contentCol) && fallbackRow.Table.Columns.Contains(contentCol) && fallbackRow[contentCol] is not DBNull)
                        {
                            var fallbackContent = fallbackRow[contentCol];
                            row[contentCol] = table.Columns[contentCol]!.DataType == typeof(byte[])
                                ? fallbackContent
                                : (fallbackContent is byte[] fallbackBytes ? Convert.ToBase64String(fallbackBytes) : fallbackContent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{System} [EnrichWithAWS] Error enriching a single row for DocumentKey={DocumentKey}", "hiso", docKey);
            }
        }
    }

    private async Task<List<DynamicParam>> GetParamListAsync(string procedureName, HealthLinkSession session, string connectionString, CancellationToken ct)
    {
        var parameters = new List<SqlParameter> { new("@pProcedureName", procedureName) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[Hiso].[USPGetProcedureParamList]", parameters, ct);

        var paramList = DataTableMapper.ToList<DynamicParam>(table);
        MapParamList(paramList, session);
        return paramList;
    }

    /// <summary>Ported directly from `DBMessages.cs`'s `MapParamList`.</summary>
    private static void MapParamList(List<DynamicParam> paramList, HealthLinkSession session)
    {
        foreach (var param in paramList)
        {
            switch (param.Parameter_name.ToLowerInvariant())
            {
                case "@patientid":
                case "@ppatientid":
                    param.ParamValue = session.PatientId;
                    break;
                case "@loggedinproviderid":
                case "@providerid":
                case "@pproviderid":
                    param.ParamValue = session.ProviderId;
                    break;
                case "@appointmentid":
                case "@pappointmentid":
                    param.ParamValue = session.AppointmentId;
                    break;
                case "@practiceid":
                case "@ppracticeid":
                    param.ParamValue = session.PracticeId;
                    break;
                case "@acc45id":
                case "@pacc45id":
                    param.ParamValue = session.ReferenceId;
                    break;
                case "@ppracticelocationid":
                    param.ParamValue = session.PracticeLocationId;
                    break;
            }
        }
    }

    /// <summary>Ported directly from `DBMessages.cs`'s inline parameter-mapping loop in `ExecuteHisoProcedure`.</summary>
    private static List<SqlParameter> CloneParams(List<SqlParameter> source) =>
        source.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToList();

    private static List<SqlParameter> MapToSqlParameters(List<DynamicParam> paramList, IReadOnlyList<HisoRequest> requests, string procedureName)
    {
        var request = requests.FirstOrDefault(r => r.ProcedureName == procedureName);
        var referenceId = string.Join(",", requests.Where(r => r.ProcedureName == procedureName && !string.IsNullOrEmpty(r.GroupreferenceID)).Select(r => r.GroupreferenceID));

        var maxVal = request is null ? null : (string.IsNullOrEmpty(request.GroupmaxVal) ? request.FieldmaxVal : request.GroupmaxVal);
        var minVal = request is null ? null : (string.IsNullOrEmpty(request.GroupminVal) ? request.FieldminVal : request.GroupminVal);
        var fromDate = request is null ? null : (string.IsNullOrEmpty(request.GroupminDateTime) ? request.FieldminDateTime : request.GroupminDateTime);
        var toDate = request is null ? null : (string.IsNullOrEmpty(request.GroupmaxDateTime) ? request.FieldmaxDateTime : request.GroupmaxDateTime);
        var search = request is null ? null : (string.IsNullOrEmpty(request.GroupsearchString) ? request.FieldsearchString : request.GroupsearchString);
        var sortBy = request is null ? null : (string.IsNullOrEmpty(request.Grouporder) ? request.Fieldorder : request.Grouporder);
        var pcode = request?.FieldQualifierID;
        var startRowIndex = request?.GroupStartRowIndex ?? 0;
        var maximumRows = request?.GroupMaximumRows ?? 0;

        var sqlParams = new List<SqlParameter>();
        foreach (var param in paramList)
        {
            switch (param.Parameter_name.ToLowerInvariant())
            {
                case "@fromdate": param.ParamValue = fromDate; break;
                case "@todate": param.ParamValue = toDate; break;
                case "@startrowindex": param.ParamValue = startRowIndex.ToString(); break;
                case "@maximumrows": param.ParamValue = maximumRows.ToString(); break;
                case "@search": param.ParamValue = search; break;
                case "@sortby": param.ParamValue = sortBy; break;
                case "@minvalue": param.ParamValue = minVal; break;
                case "@maxvalue": param.ParamValue = maxVal; break;
                case "@pcode":
                case "@vcode": param.ParamValue = pcode; break;
                case "@referenceid": param.ParamValue = string.IsNullOrEmpty(referenceId) ? null : referenceId; break;
            }

            sqlParams.Add(new SqlParameter(param.Parameter_name, (object?)param.ParamValue ?? DBNull.Value));
        }

        return sqlParams;
    }
}
