namespace HekCoreApi.Contracts.Auth;

/// <summary>
/// Canonical POST /auth/token request shape (OpenAPI: TokenRequest). Edge adapters translate each
/// legacy consumer's existing Authenticate payload into this shape before dispatch.
/// </summary>
public sealed record TokenRequest(
    string Username,
    string Password,
    int? PatientId = null,
    int? EncounterId = null,
    string? PracticeId = null);
