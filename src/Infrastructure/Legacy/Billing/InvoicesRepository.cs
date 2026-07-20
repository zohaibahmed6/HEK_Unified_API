using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Billing;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Billing;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories. The legacy random-6-digit-pre-confirmation-code artifact (ERMS-BR-16) is intentionally NOT reproduced (Contract Design doc Section 6.2 - "confirmed legacy artifact, not a rule to preserve").</summary>
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

    public async Task<Invoice> SaveAsync(int patientId, string practiceId, InvoiceInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pServiceCode", input.ServiceCode),
            new("@pServiceName", (object?)input.ServiceName ?? DBNull.Value),
            new("@pAmountInclGST", input.AmountInclGst),
            new("@pPayee", (object?)input.Payee ?? DBNull.Value),
            new("@pServiceProvider", (object?)input.ServiceProvider ?? DBNull.Value),
            new("@pServiceDate", (object?)input.ServiceDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
            new("@pPegasusReference", (object?)input.PegasusReference ?? DBNull.Value),
            new("@pClaimShortCode", (object?)input.ClaimShortCode ?? DBNull.Value)
        };

        var output = new SqlParameter("@pInvoiceIDOut", SqlDbType.NVarChar, 64) { Direction = ParameterDirection.Output };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveInvoice]", parameters, ct);

        return Invoice.FromInput(output.Value?.ToString() ?? Guid.NewGuid().ToString(), "created", input);
    }

    private static Invoice MapRow(DataRow row) => new(
        row["InvoiceId"]?.ToString() ?? Guid.NewGuid().ToString(),
        "existing",
        row["ServiceCode"]?.ToString() ?? string.Empty,
        row["ServiceName"] is DBNull or null ? null : row["ServiceName"].ToString(),
        row["AmountInclGST"] is DBNull or null ? 0 : Convert.ToDecimal(row["AmountInclGST"]),
        row["Payee"] is DBNull or null ? null : row["Payee"].ToString(),
        row["ServiceProvider"] is DBNull or null ? null : row["ServiceProvider"].ToString(),
        row["ServiceDate"] is DBNull or null ? null : DateOnly.FromDateTime(Convert.ToDateTime(row["ServiceDate"])),
        row["PegasusReference"] is DBNull or null ? null : row["PegasusReference"].ToString(),
        row["ClaimShortCode"] is DBNull or null ? null : row["ClaimShortCode"].ToString());
}
