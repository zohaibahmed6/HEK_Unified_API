using System.Data;
using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Billing;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Billing;

/// <summary>
/// `SaveAsync` calls the real, confirmed legacy stored procedure and parameter set - sourced
/// directly from legacy-reference/controller/COLController.cs's `SaveInvoice()` action (the JSON
/// request-to-DAL-call mapping) and legacy-reference/DAL/HSS/HSSDA.cs's `InsertUpdateService`
/// (the DAL call itself). PROJECT_STATUS.md open item 17, closed for the ERMS/COL calling path.
///
/// Confirmed, not inferred: procedure name `[OnlineClaim].[uspInsertUpdateService]`; `@pMasterServiceName`
/// is hardcoded to the literal `"COL"` by the legacy controller (not caller-supplied) - carried forward
/// unchanged since it's what the real code does, not a guess; `@pSubServiceName`/`@pSubServiceCode` map
/// from this contract's `ServiceName`/`ServiceCode` (note the DAL's own naming reverses "master" vs
/// "sub" relative to this contract's field names); `@pFee` takes the amount as a caller-supplied plain
/// string in the legacy code (no GST math) - passed as `AmountInclGst.ToString()` here since this
/// contract keeps `AmountInclGst` typed as `decimal` at the API boundary (a deliberate, non-business-rule
/// typing improvement - the value itself is unchanged); `@pLocationId` is hardcoded empty/DBNull by the
/// legacy controller too (never caller-supplied), preserved the same way. The stored procedure's own
/// `-3` return code ("already exists") is still recognized here defensively, but is not this platform's
/// primary duplicate-detection mechanism - see `FindByNaturalKeyAsync` below and Contract Design doc
/// Section 12 Decision 3 (FR-IDEM-01), which deliberately replaces KARO's/ERMS COL's magic-number
/// convention with a documented Idempotency-Key/natural-key contract instead.
///
/// `FindByNaturalKeyAsync`'s procedure/columns remain a FLAGGED INFERENCE (no live-schema access,
/// same caveat as every other Block 2 repository, PROJECT_STATUS.md open item 28) - the legacy system
/// never needed a separate lookup, since its own idempotency lived inside the write procedure's `-3`
/// signal; this platform's natural-key fallback path is new capability, not a legacy port.
/// </summary>
public sealed class InvoicesRepository : IInvoicesRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public InvoicesRepository(ILegacyPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<Invoice?> FindByNaturalKeyAsync(int patientId, string practiceId, string serviceCode, DateOnly? serviceDate, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pServiceCode", serviceCode),
            new("@pServiceDate", (object?)serviceDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspFindInvoiceByNaturalKey]", parameters, ct);
        return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
    }

    public async Task<(Invoice Invoice, bool WasDuplicate)> SaveAsync(int patientId, string? encounterId, string practiceId, InvoiceInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pAccountHolderID", (object?)input.AccountHolderId ?? DBNull.Value),
            new("@pAppointmentId", (object?)encounterId ?? DBNull.Value),
            new("@pMasterServiceName", "COL"),
            new("@pSubServiceName", (object?)input.ServiceName ?? DBNull.Value),
            new("@pSubServiceCode", input.ServiceCode),
            new("@pFee", input.AmountInclGst.ToString(CultureInfo.InvariantCulture)),
            new("@pLocationId", DBNull.Value),
            new("@pDescription", (object?)input.Description ?? DBNull.Value),
            new("@pPayee", (object?)input.Payee ?? DBNull.Value),
            new("@pServiceProvider", (object?)input.ServiceProvider ?? DBNull.Value),
            new("@pServiceProviderType", (object?)input.ServiceProviderType ?? DBNull.Value),
            new("@pServiceDate", (object?)input.ServiceDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
            new("@pPegasusReference", (object?)input.PegasusReference ?? DBNull.Value),
            new("@pClaimShortCode", (object?)input.ClaimShortCode ?? DBNull.Value)
        };

        var output = new SqlParameter("@pOutputParam", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[OnlineClaim].[uspInsertUpdateService]", parameters, ct);

        var serviceMappingId = output.Value is DBNull or null ? -1 : Convert.ToInt64(output.Value);

        if (serviceMappingId == -3)
        {
            return (Invoice.FromInput(Guid.NewGuid().ToString(), "existing", input), true);
        }

        if (serviceMappingId <= 0)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(input.ServiceCode), "Invalid values passed to the legacy invoice-save procedure.") });
        }

        return (Invoice.FromInput(serviceMappingId.ToString(CultureInfo.InvariantCulture), "created", input), false);
    }

    private static Invoice MapRow(DataRow row) => new(
        row["InvoiceId"]?.ToString() ?? Guid.NewGuid().ToString(),
        "existing",
        row["ServiceCode"]?.ToString() ?? string.Empty,
        row["ServiceName"] is DBNull or null ? null : row["ServiceName"].ToString(),
        row["AmountInclGST"] is DBNull or null ? 0 : Convert.ToDecimal(row["AmountInclGST"]),
        row["Description"] is DBNull or null ? null : row["Description"].ToString(),
        row["AccountHolderID"] is DBNull or null ? null : row["AccountHolderID"].ToString(),
        row["Payee"] is DBNull or null ? null : row["Payee"].ToString(),
        row["ServiceProvider"] is DBNull or null ? null : row["ServiceProvider"].ToString(),
        row["ServiceProviderType"] is DBNull or null ? null : row["ServiceProviderType"].ToString(),
        row["ServiceDate"] is DBNull or null ? null : DateOnly.FromDateTime(Convert.ToDateTime(row["ServiceDate"])),
        row["PegasusReference"] is DBNull or null ? null : row["PegasusReference"].ToString(),
        row["ClaimShortCode"] is DBNull or null ? null : row["ClaimShortCode"].ToString());
}
