namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// The "full Provider/Patient/Appointment/Practice/ACC45-reference context" HISO-BR-01 says a
/// SessionGUID resolves into. <see cref="ReferenceId"/> (ACC45 reference) and
/// <see cref="PracticeLocationId"/> extend Block 1's <see cref="HisoSessionContext"/> (the raw
/// tblHealthLinkSession row) - the session-resolution logic that populates these two extra fields
/// is not part of the supplied legacy source, so they are left null/unset for now and flagged as
/// an incomplete mapping, not a confirmed-empty one.
/// <see cref="SessionKey"/> is the raw SOAP `sessionKey` GUID itself - added 2026-07-30 after a live
/// test with real data (session `439cc902-...`, ACC45 row `650854`) proved
/// `Acc45DefinitionRepository.GetDefinitionAsync` needs the actual session GUID for its
/// `@pSessionKey` parameter (confirmed against the real legacy source,
/// `Acc45DefinitionBuilder.cs:145` - `objSession.GUID.ToString()`, sent unconditionally). Optional so
/// every canonical-hub call site that builds a `HealthLinkSession` without a HISO session (patient/
/// encounter scope only, no SessionGUID concept) is unaffected.
/// </summary>
public sealed record HealthLinkSession(
    string PatientId,
    string ProviderId,
    string AppointmentId,
    string PracticeId,
    string? ReferenceId = null,
    string? PracticeLocationId = null,
    string? PracticeEdi = null,
    Guid? SessionKey = null)
{
    public static HealthLinkSession FromSessionContext(HisoSessionContext context, Guid sessionKey) =>
        new(context.PatientId, context.ProviderId, context.AppointmentId, context.PracticeId, PracticeEdi: context.PracticeEdi, SessionKey: sessionKey);
}
