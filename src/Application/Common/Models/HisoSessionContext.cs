namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// Resolved [Appointment].[tblHealthLinkSession] row (HISO-BR-01) - the documented columns only
/// (ProviderID, PatientID, AppointmentID, PracticeID) plus a creation timestamp used for the new
/// 12-hour expiry enforcement (ADR-004). No schema changes to the legacy table (ADR-010).
/// </summary>
public sealed record HisoSessionContext(
    string ProviderId,
    string PatientId,
    string AppointmentId,
    string PracticeId,
    DateTimeOffset SessionCreatedAtUtc);
