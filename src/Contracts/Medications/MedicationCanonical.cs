using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Medications;

/// <summary>
/// Unified medications shape spanning HISO/KARO/ERMS. KARO returns one flat list (`[HSS].[uspGetMedications]`
/// with no `@pIsLongTerm` filter - both prescribed and regular medications together, `IsLongTerm` still
/// distinguishes them). ERMS/HISO split the same real data into two calls/concepts by `IsLongTerm`
/// (ERMS: `uspGetMedications @pIsLongTerm=0/1`; HISO: `Patient_PrescribedMedication`/`Patient_RegularMedication`,
/// both confirmed real against patient 2459731 in a prior session's empirical check) - concatenated here
/// into one list, same as KARO's single real call already returns.
/// </summary>
public sealed record MedicationCanonical(
    string? SctId,
    string Name,
    string? Dosage,
    string? Directions,
    string? StartDate,
    bool IsLongTerm,
    OriginScope Source);
