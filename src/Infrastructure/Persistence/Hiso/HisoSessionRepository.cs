using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Persistence.Hiso;

/// <summary>
/// Read-only query against the existing, unchanged [Appointment].[tblHealthLinkSession]
/// (ProviderID, PatientID, AppointmentID, PracticeID - the columns confirmed by HISO-BR-01 and
/// ADR-010's evidence). No schema changes. Connection string is resolved per server address via
/// ISecretProvider, never hardcoded - direct replacement for the legacy pattern this project exists
/// to retire (SRS Section 12.10). Always parameterized - never string concatenation.
///
/// FLAGGED INFERENCE: a session-creation-timestamp column (referenced below as CreatedAtUtc) is
/// required to enforce the new 12-hour expiry (ADR-004) but is not named in any source document -
/// only ProviderID/PatientID/AppointmentID/PracticeID are confirmed present. The actual column name
/// (and whether one exists at all) needs confirming against the live schema before this is trusted.
/// </summary>
public sealed class HisoSessionRepository : IHisoSessionRepository
{
    private readonly ISecretProvider _secretProvider;

    public HisoSessionRepository(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public async Task<HisoSessionContext?> FindBySessionGuidAsync(Guid sessionGuid, string serverAddress, CancellationToken ct = default)
    {
        var connectionString = await _secretProvider.GetSecretAsync($"Hiso:ConnectionStrings:{serverAddress}", ct);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        const string sql = """
            SELECT ProviderID, PatientID, AppointmentID, PracticeID, CreatedAtUtc
            FROM [Appointment].[tblHealthLinkSession]
            WHERE SessionGUID = @SessionGuid
            """;

        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SessionGuid", sessionGuid);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new HisoSessionContext(
            ProviderId: reader["ProviderID"].ToString() ?? string.Empty,
            PatientId: reader["PatientID"].ToString() ?? string.Empty,
            AppointmentId: reader["AppointmentID"].ToString() ?? string.Empty,
            PracticeId: reader["PracticeID"].ToString() ?? string.Empty,
            SessionCreatedAtUtc: reader["CreatedAtUtc"] is DateTimeOffset dto ? dto : (DateTime)reader["CreatedAtUtc"]);
    }
}
