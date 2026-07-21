using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>Ported from ERMS's `HSSDA.InsertAndValidateToken` call (8-parameter overload incl. `expiryInDays`) - same real `[HSS].[uspInsertAndValidateToken]` proc as KARO.</summary>
public sealed class ErmsAuthRepository : IErmsAuthRepository
{
    private readonly IErmsPracticeConnectionResolver _connectionResolver;

    public ErmsAuthRepository(IErmsPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<KaroAuthResult?> InsertAndValidateTokenAsync(string practiceSuffix, string? username, string? password, string? patientId, string? encounterId, string? token = null, double expiryInDays = 0, string? pho = null, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);

        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(username))
        {
            parameters.Add(new SqlParameter("@pUsername", username));
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            parameters.Add(new SqlParameter("@pPassword", password));
        }

        if (!string.IsNullOrWhiteSpace(patientId))
        {
            parameters.Add(new SqlParameter("@pPatientID", patientId));
        }

        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            parameters.Add(new SqlParameter("@pAppointmentID", encounterId));
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            parameters.Add(new SqlParameter("@pToken", token));
        }

        if (!string.IsNullOrWhiteSpace(pho))
        {
            parameters.Add(new SqlParameter("@pPHO", pho));
        }

        if (expiryInDays != 0)
        {
            parameters.Add(new SqlParameter("@pExpiryInDays", expiryInDays));
        }

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspInsertAndValidateToken]", parameters, ct);
        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new KaroAuthResult(
            table.Columns.Contains("StatusMessage") && row["StatusMessage"] is not DBNull ? row["StatusMessage"].ToString() : null,
            table.Columns.Contains("Expiry") && row["Expiry"] is not DBNull && DateTime.TryParse(row["Expiry"].ToString(), out var expiry) ? expiry : null,
            table.Columns.Contains("Token") && row["Token"] is not DBNull ? row["Token"].ToString() : null,
            table.Columns.Contains("PracticeId") && row["PracticeId"] is not DBNull ? row["PracticeId"].ToString() : null);
    }
}
