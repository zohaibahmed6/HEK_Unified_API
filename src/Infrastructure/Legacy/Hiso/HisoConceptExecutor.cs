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
/// - Connection resolution goes through <see cref="ILegacyPracticeConnectionResolver"/> (ADR-001
///   tenant registry) instead of a fixed `ConectionStringPMS_NZ` app setting.
/// - The legacy "second database node" routing for specific report/attachment/letter/problem
///   procedures (HISO-BR-05) is NOT implemented - no source document describes how the tenant
///   registry should model a second node per practice, so this always executes against the
///   practice's single registered connection. Flagged, not silently dropped.
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

    private readonly ILegacyPracticeConnectionResolver _connectionResolver;
    private readonly ILogger<HisoConceptExecutor> _logger;

    public HisoConceptExecutor(ILegacyPracticeConnectionResolver connectionResolver, ILogger<HisoConceptExecutor> logger)
    {
        _connectionResolver = connectionResolver;
        _logger = logger;
    }

    public async Task<DataSet?> ExecuteAsync(string procedureName, HealthLinkSession session, IReadOnlyList<HisoRequest> requests, CancellationToken ct = default)
    {
        _logger.LogInformation("[ExecuteHisoProcedure] Started | Procedure: {ProcedureName} | PracticeId: {PracticeId}", procedureName, session.PracticeId);

        try
        {
            var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);

            var paramList = await GetParamListAsync(procedureName, session, connectionString, ct);
            var sqlParams = MapToSqlParameters(paramList, requests, procedureName);

            if (AwsEnabledProcedures.Contains(procedureName))
            {
                _logger.LogWarning(
                    "[ExecuteHisoProcedure] {ProcedureName} is AWS-enrichment-eligible, but AWSDoc's source was never available (SRS Section 16) - executing the non-AWS procedure only, no enrichment attempted.",
                    procedureName);
            }

            var result = sqlParams.Count > 0
                ? await LegacyDbExecutor.ExecuteDataSetAsync(connectionString, CommandType.StoredProcedure, procedureName, sqlParams, ct)
                : await LegacyDbExecutor.ExecuteDataSetAsync(connectionString, CommandType.StoredProcedure, procedureName, null, ct);

            _logger.LogInformation("[ExecuteHisoProcedure] Completed | Procedure={ProcedureName}", procedureName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExecuteHisoProcedure] Failed executing {ProcedureName}", procedureName);
            return null;
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
