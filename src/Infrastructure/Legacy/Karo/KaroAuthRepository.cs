using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>
/// Ported from `HSSDA.InsertAndValidateToken` (8-parameter real overload) - calls the real
/// `[HSS].[uspInsertAndValidateToken]`.
/// </summary>
public sealed class KaroAuthRepository : IKaroAuthRepository
{
    private readonly IKaroPracticeConnectionResolver _connectionResolver;

    public KaroAuthRepository(IKaroPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<KaroAuthResult?> InsertAndValidateTokenAsync(
        string practiceSuffix, RoutingContext routingContext, string? username, string? password, string? patientId,
        string? appointmentId, string? token, string? pho, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);

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

        if (!string.IsNullOrWhiteSpace(appointmentId))
        {
            parameters.Add(new SqlParameter("@pAppointmentID", appointmentId));
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            parameters.Add(new SqlParameter("@pToken", token));
        }

        if (!string.IsNullOrWhiteSpace(pho))
        {
            parameters.Add(new SqlParameter("@pPHO", pho));
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
