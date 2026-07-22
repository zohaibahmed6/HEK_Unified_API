using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Conditions;

/// <summary>
/// Unified conditions/problem-list shape spanning HISO/KARO/ERMS. KARO and ERMS share the identical
/// real procedure (`[HSS].[uspGetConditions]`, confirmed in both `KaroDataRepository`/`ErmsDataRepository`)
/// so their fields line up exactly; HISO uses the real, confirmed `Patient_Problem` concept group
/// (`Hiso.uspGetPatient_Problem`).
/// </summary>
public sealed record ConditionCanonical(
    string? ConceptId,
    string Name,
    string? Summary,
    string? DiagnosisDate,
    bool IsLongTerm,
    OriginScope Source);
