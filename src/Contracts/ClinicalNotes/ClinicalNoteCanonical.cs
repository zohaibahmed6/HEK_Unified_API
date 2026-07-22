using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.ClinicalNotes;

/// <summary>
/// Unified clinical/consult notes shape spanning HISO/KARO/ERMS. KARO and ERMS share the identical
/// real procedure (`[HSS].[uspGetConsultNotes]`, confirmed live against patient 2459731) - it returns
/// two parallel column sets (plain `subjectiveNotes`/`objectiveNotes`/etc. and a second pipe-delimited
/// composite-reference set); the plain columns are the real usable ones, confirmed by comparing
/// values directly (same "dual column" pattern already found on KARO's demographics). HISO uses the
/// real, confirmed `Patient_Consult` concept group.
/// </summary>
public sealed record ClinicalNoteCanonical(
    string? ReferenceId,
    string? SubjectiveNotes,
    string? ObjectiveNotes,
    string? Assessment,
    string? Plans,
    string? AppointmentAdvice,
    string? Date,
    OriginScope Source);
