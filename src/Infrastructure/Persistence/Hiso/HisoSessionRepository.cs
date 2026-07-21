using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Persistence.Hiso;

/// <summary>
/// Calls the real legacy `[Appointment].[usptblHealthLinkSession_GetByGUID]` (confirmed against the
/// real PMS_NZ_V2 database, 2026-07-21: returns AppointmentID/PatientID/ProviderID/PracticeID/
/// PMSReferenceID/SessionGUID/PracticeLocationID/EDIAccount - EDIAccount is proc-computed, not a raw
/// column on `tblHealthLinkSession`). No schema changes. Connection string is resolved per server
/// address via ISecretProvider, never hardcoded - direct replacement for the legacy pattern this
/// project exists to retire (SRS Section 12.10). Always parameterized - never string concatenation.
///
/// The real proc does not expose a session-creation timestamp, so the new 12-hour expiry (ADR-004,
/// not part of legacy) is enforced via a second, minimal parameterized read of `InsertedAt` directly
/// from `[Appointment].[tblHealthLinkSession]` (confirmed real column name) - kept separate from the
/// proc call rather than guessed into the proc's output shape.
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

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand("[Appointment].[usptblHealthLinkSession_GetByGUID]", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@SessionGUID", sessionGuid);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        var providerId = reader["ProviderID"].ToString() ?? string.Empty;
        var patientId = reader["PatientID"].ToString() ?? string.Empty;
        var appointmentId = reader["AppointmentID"].ToString() ?? string.Empty;
        var practiceId = reader["PracticeID"].ToString() ?? string.Empty;
        var practiceEdi = reader["EDIAccount"] is DBNull ? null : reader["EDIAccount"].ToString();
        await reader.DisposeAsync();

        var insertedAt = await GetInsertedAtAsync(connection, sessionGuid, ct);

        return new HisoSessionContext(
            ProviderId: providerId,
            PatientId: patientId,
            AppointmentId: appointmentId,
            PracticeId: practiceId,
            SessionCreatedAtUtc: insertedAt,
            PracticeEdi: practiceEdi);
    }

    private static async Task<DateTimeOffset> GetInsertedAtAsync(SqlConnection connection, Guid sessionGuid, CancellationToken ct)
    {
        await using var command = new SqlCommand(
            "SELECT InsertedAt FROM [Appointment].[tblHealthLinkSession] WHERE SessionGUID = @SessionGuid", connection);
        command.Parameters.AddWithValue("@SessionGuid", sessionGuid);

        var result = await command.ExecuteScalarAsync(ct);
        return result switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            _ => DateTimeOffset.UtcNow
        };
    }
}
