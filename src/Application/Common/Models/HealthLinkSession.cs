namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// The "full Provider/Patient/Appointment/Practice/ACC45-reference context" HISO-BR-01 says a
/// SessionGUID resolves into. <see cref="ReferenceId"/> (ACC45 reference) and
/// <see cref="PracticeLocationId"/> extend Block 1's <see cref="HisoSessionContext"/> (the raw
/// tblHealthLinkSession row) - the session-resolution logic that populates these two extra fields
/// is not part of the supplied legacy source, so they are left null/unset for now and flagged as
/// an incomplete mapping, not a confirmed-empty one.
/// </summary>
public sealed record HealthLinkSession(
    string PatientId,
    string ProviderId,
    string AppointmentId,
    string PracticeId,
    string? ReferenceId = null,
    string? PracticeLocationId = null)
{
    public static HealthLinkSession FromSessionContext(HisoSessionContext context) =>
        new(context.PatientId, context.ProviderId, context.AppointmentId, context.PracticeId);
}
