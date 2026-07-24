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
    private readonly IAwsDocumentService _awsDocumentService;
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<HisoConceptExecutor> _logger;

    public HisoConceptExecutor(IHisoPracticeConnectionResolver connectionResolver, ILegacyPracticeConnectionResolver legacyConnectionResolver, IAwsDocumentService awsDocumentService, ISecretProvider secretProvider, ILogger<HisoConceptExecutor> logger)
    {
        _connectionResolver = connectionResolver;
        _legacyConnectionResolver = legacyConnectionResolver;
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
                            // everything else. Falls back to the PMS connection if unconfigured, same
                            // as the earlier flagged assumption, rather than hard-failing.
                            var dmsConnectionString = await _secretProvider.GetSecretAsync("Hiso:DmsConnectionString", ct) ?? dataConnectionString;
                            await EnrichWithAwsAsync(awsResult.Tables[0], practiceIdInt, referenceId, dmsConnectionString, dataConnectionString, ct);
                            _logger.LogInformation("[ExecuteHisoProcedure] Completed (AWS flow) | Procedure={ProcedureName}", awsProcedureName);
                            return awsResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Legacy: AWS flow failure falls back to the plain procedure rather than
                        // failing the whole request - reproduced exactly.
                        _logger.LogWarning(ex, "[ExecuteHisoProcedure] AWS flow failed for {ProcedureName}, falling back to the plain procedure.", awsProcedureName);
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
            _logger.LogError(ex, "[ExecuteHisoProcedure] Failed executing {ProcedureName}", procedureName);
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
    /// database (`Hiso:DmsConnectionString`), not the practice's main PMS_NZ connection.
    /// </summary>
    private async Task EnrichWithAwsAsync(DataTable table, int practiceId, string referenceId, string dmsConnectionString, string pmsConnectionString, CancellationToken ct)
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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[EnrichWithAWS] Error enriching a single row for DocumentKey={DocumentKey}", docKey);
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
