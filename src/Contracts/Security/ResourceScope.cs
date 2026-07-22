namespace HekCoreApi.Contracts.Security;

/// <summary>
/// Token-claim shape (ADR-003) - never a wire request/response body. A token is valid for exactly
/// one patient + encounter + practice combination, plus a structurally-determined origin scope.
/// </summary>
public sealed record ResourceScope(
    string PatientId,
    string? EncounterId,
    string PracticeId,
    OriginScope OriginScope,
    string? PracticeCode = null,
    string? Environment = null);
