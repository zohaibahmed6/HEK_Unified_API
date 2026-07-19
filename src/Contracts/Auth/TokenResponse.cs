using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Auth;

/// <summary>
/// Canonical POST /auth/token response shape (OpenAPI: TokenResponse).
/// </summary>
public sealed record TokenResponse(
    string Token,
    DateTimeOffset Expiry,
    string PracticeId,
    OriginScope OriginScope);
