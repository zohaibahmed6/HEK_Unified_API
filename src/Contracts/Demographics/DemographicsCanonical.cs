using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Demographics;

/// <summary>
/// Unified demographics shape (HEK_UNIFIED_API_SPEC.md FR-2/FR-3) - one canonical model spanning
/// HISO/KARO/ERMS/COL. Fields not produced by the source system are null. Field-level scoping is
/// applied on top of this by FieldSelector before serialization (FR-5).
/// </summary>
public sealed record DemographicsCanonical(
    int PatientId,
    string? PracticeId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    DateOnly? DateOfEnrolment,
    DateOnly? EndEnrolmentDate,
    int? EncounterId,
    string? Nhi,
    OriginScope Source);
