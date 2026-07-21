using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>
/// Ported from ERMS's `HSSDA` read calls (`DAL/South/HSSDA.cs`) - real procs on
/// `ConnIndiciDB{practiceid}`. Sparse-parameter rules reproduced exactly (sort/date/optional
/// params only added when non-blank / not MinValue).
/// </summary>
public sealed class ErmsDataRepository : IErmsDataRepository
{
    private readonly IErmsPracticeConnectionResolver _connectionResolver;

    public ErmsDataRepository(IErmsPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public Task<DataTable> GetMeasurementAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetMeasurement]", PatientOnly(patientId), ct);

    public Task<DataTable> GetSmokingStatusAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetSmokingStatus]", PatientOnly(patientId), ct);

    public Task<DataTable> GetProviderAsync(string practiceSuffix, string? patientId, string? userId, string? locationId, string? encounterId, CancellationToken ct = default)
    {
        var parameters = PatientOnly(patientId);
        if (!string.IsNullOrWhiteSpace(locationId))
        {
            parameters.Add(new SqlParameter("@pLocationId", locationId));
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            parameters.Add(new SqlParameter("@pUserId", userId));
        }

        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            parameters.Add(new SqlParameter("@pEncounterid", encounterId));
        }

        return ExecuteAsync(practiceSuffix, "[HSS].[uspGetProvider]", parameters, ct);
    }

    public Task<DataTable> GetNextOfKinAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetNextOfKin]", PatientOnly(patientId), ct);

    public Task<DataTable> GetRegisteredPractitionersAsync(string practiceSuffix, string? patientId, string? locationId, CancellationToken ct = default)
    {
        var parameters = PatientOnly(patientId);
        if (!string.IsNullOrWhiteSpace(locationId))
        {
            parameters.Add(new SqlParameter("@pLocationId", locationId));
        }

        return ExecuteAsync(practiceSuffix, "[HSS].[uspGetRegisteredPractitioners]", parameters, ct);
    }

    public Task<DataTable> GetAcc45Async(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetACC45]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetConditionsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetConditions]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetConsultNotesAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetConsultNotes]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetMedicalAllergiesAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetAllergies]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetMedicationsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isLongTerm, CancellationToken ct = default)
    {
        var parameters = Dated(patientId, sortOrder, minDate, maxDate);
        parameters.Add(new SqlParameter("@pIsLongTerm", isLongTerm));
        parameters.Add(new SqlParameter("@pShowStop", false));
        return ExecuteAsync(practiceSuffix, "[HSS].[uspGetMedications]", parameters, ct);
    }

    public Task<DataTable> GetLabsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetLabs]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetLabResultsAsync(string practiceSuffix, string? patientId, string? referenceId, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetLabResults]", Detail(patientId, referenceId), ct);

    public Task<DataTable> GetRadsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetRads]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetRadResultsAsync(string practiceSuffix, string? patientId, string? referenceId, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[HSS].[uspGetRadResults]", Detail(patientId, referenceId), ct);

    public Task<DataTable> GetOtherDocsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isReferral, CancellationToken ct = default)
    {
        var parameters = Dated(patientId, sortOrder, minDate, maxDate);
        if (isReferral)
        {
            parameters.Add(new SqlParameter("@pType", "Discharge Summary"));
        }

        return ExecuteAsync(practiceSuffix, "[HSS].[uspGetOtherDocs]", parameters, ct);
    }

    public Task<DataTable> GetDocResultsAsync(string practiceSuffix, string? referenceId, bool isDischarge, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pIsDischarge", isDischarge) };
        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            parameters.Add(new SqlParameter("@pReferenceId", referenceId));
        }

        return ExecuteAsync(practiceSuffix, "[HSS].[uspGetDocResults]", parameters, ct);
    }

    /// <summary>Legacy `GetLabResults`/`GetRadResults`: both params only added when non-blank.</summary>
    private static List<SqlParameter> Detail(string? patientId, string? referenceId)
    {
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(patientId))
        {
            parameters.Add(new SqlParameter("@pPatientId", patientId));
        }

        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            parameters.Add(new SqlParameter("@pReferenceId", referenceId));
        }

        return parameters;
    }

    private static List<SqlParameter> PatientOnly(string? patientId) =>
        new() { new SqlParameter("@pPatientId", (object?)patientId ?? DBNull.Value) };

    private static List<SqlParameter> Dated(string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate)
    {
        var parameters = PatientOnly(patientId);
        if (!string.IsNullOrWhiteSpace(sortOrder))
        {
            parameters.Add(new SqlParameter("@pSortOrder", sortOrder));
        }

        if (!minDate.Equals(DateTime.MinValue))
        {
            parameters.Add(new SqlParameter("@pMinDate", minDate));
        }

        if (!maxDate.Equals(DateTime.MinValue))
        {
            parameters.Add(new SqlParameter("@pMaxDate", maxDate));
        }

        return parameters;
    }

    private async Task<DataTable> ExecuteAsync(string practiceSuffix, string procName, List<SqlParameter> parameters, CancellationToken ct)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);
        return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procName, parameters, ct);
    }
}
