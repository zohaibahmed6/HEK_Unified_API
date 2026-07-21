using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>Ported from `DAL/Pegasus/PHCO.cs` + `HSSDA.InsertUpdateService` (`[OnlineClaim]` schema procs), on the shared `ConnIndiciDB{practiceid}` routing.</summary>
public sealed class ColDataRepository : IColDataRepository
{
    private readonly IErmsPracticeConnectionResolver _connectionResolver;

    public ColDataRepository(IErmsPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public Task<DataTable> GetCurrentPatientDataAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[OnlineClaim].[uspGetPatientData]", PatientOnly(patientId), ct);

    public Task<DataTable> GetProviderDataAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(practiceSuffix, "[OnlineClaim].[uspGetProvider]", PatientOnly(patientId), ct);

    public Task<DataTable> GetSessionDataAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        // Legacy BUG reproduced on purpose: PHCO.GetSessionData executes an EMPTY proc name, which
        // always fails - the SQL error message becomes the legacy response body.
        ExecuteAsync(practiceSuffix, "", PatientOnly(patientId), ct);

    public Task<DataTable> GetDiagnosisDataAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default)
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

        return ExecuteAsync(practiceSuffix, "[OnlineClaim].[uspGetConditions]", parameters, ct);
    }

    public Task<DataTable> GetSurgeryDataAsync(string practiceSuffix, string? patientId, string? locationId, string? encounterId, CancellationToken ct = default)
    {
        var parameters = PatientOnly(patientId);
        parameters.Add(new SqlParameter("@pEncounterId", (object?)encounterId ?? DBNull.Value));
        if (!string.IsNullOrWhiteSpace(locationId))
        {
            parameters.Add(new SqlParameter("@pLocationId", locationId));
        }

        return ExecuteAsync(practiceSuffix, "[OnlineClaim].[uspGetSurgeryData]", parameters, ct);
    }

    public async Task<long> SaveInvoiceAsync(string practiceSuffix, string? patientId, string? accountHolderId, string? encounterId, string? serviceName, string? serviceCode,
        string? fee, string? description, string? payee, string? serviceProvider, string? serviceProviderType, string? serviceDate, string? pegasusReference, string? claimShortCode, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);

        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", Convert.ToInt32(patientId)),
            new("@pAppointmentId", (object?)encounterId ?? DBNull.Value),
            new("@pMasterServiceName", "COL"),
            new("@pSubServiceName", (object?)serviceName ?? DBNull.Value)
        };
        if (!string.IsNullOrWhiteSpace(accountHolderId))
        {
            parameters.Add(new SqlParameter("@pAccountHolderID", accountHolderId));
        }

        if (!string.IsNullOrWhiteSpace(serviceCode))
        {
            parameters.Add(new SqlParameter("@pSubServiceCode", serviceCode));
        }

        if (!string.IsNullOrWhiteSpace(fee))
        {
            parameters.Add(new SqlParameter("@pFee", fee));
        }

        // Legacy: COL always passes locationId = "" -> explicit DBNull; blank optionals below are DBNull too.
        parameters.Add(new SqlParameter("@pLocationId", DBNull.Value));
        parameters.Add(new SqlParameter("@pDescription", BlankToNull(description)));
        parameters.Add(new SqlParameter("@pPayee", BlankToNull(payee)));
        parameters.Add(new SqlParameter("@pServiceProvider", BlankToNull(serviceProvider)));
        parameters.Add(new SqlParameter("@pServiceProviderType", BlankToNull(serviceProviderType)));
        parameters.Add(new SqlParameter("@pServiceDate", BlankToNull(serviceDate)));
        parameters.Add(new SqlParameter("@pPegasusReference", BlankToNull(pegasusReference)));
        parameters.Add(new SqlParameter("@pClaimShortCode", BlankToNull(claimShortCode)));

        var outParam = new SqlParameter("@pOutputParam", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(outParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[OnlineClaim].[uspInsertUpdateService]", parameters, ct);
        return outParam.Value is DBNull or null ? 0 : Convert.ToInt64(outParam.Value);
    }

    private static object BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    private static List<SqlParameter> PatientOnly(string? patientId) =>
        new() { new SqlParameter("@pPatientId", (object?)patientId ?? DBNull.Value) };

    private async Task<DataTable> ExecuteAsync(string practiceSuffix, string procName, List<SqlParameter> parameters, CancellationToken ct)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);
        return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procName, parameters, ct);
    }
}
