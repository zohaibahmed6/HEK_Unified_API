using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.LabResults;

/// <summary>
/// Unified lab-result shape spanning HISO/KARO/ERMS. KARO and ERMS share the identical real
/// procedure (`[HSS].[uspGetLabResults]`, confirmed live against patient 2459731) returning
/// individual test-result rows (name/value/unit/reference range). HISO's real `Patient_LaboratoryReport`
/// concept (confirmed against the dictionary - `Hiso.uspGetPatient_LaboratoryReport`) is report/document
/// level, not individual-test level - it has no `Value`/`Unit`/`ReferenceRange` concept fields at all, so
/// those are correctly absent from HISO's allowed fields (same documented-gap pattern as Documents).
/// </summary>
public sealed record LabResultCanonical(
    string? ReferenceId,
    string? TestName,
    string? Subject,
    string? Value,
    string? Unit,
    string? ReferenceRange,
    string? Comments,
    string? Date,
    OriginScope Source);
