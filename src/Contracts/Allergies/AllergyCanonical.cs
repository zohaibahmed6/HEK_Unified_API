using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Allergies;

/// <summary>
/// Unified allergy shape spanning HISO/ERMS. KARO has no allergies operation at all (confirmed from
/// the real compat controller - a genuine gap, not an oversight), so KARO-scoped tokens get a clean
/// 501 rather than an invented mapping. ERMS's real `[HSS].[uspGetAllergies]` (confirmed live against
/// patient 2459731) and HISO's real `Patient_Allergy` concept (confirmed empirically in a prior
/// session against the confusingly-similar `Patient_MedicalWarning`, which returned mostly-NULL data
/// for this patient) both resolve to the same real fields.
/// </summary>
public sealed record AllergyCanonical(
    string? ReferenceId,
    string? Description,
    string? Comments,
    string? Date,
    OriginScope Source);
