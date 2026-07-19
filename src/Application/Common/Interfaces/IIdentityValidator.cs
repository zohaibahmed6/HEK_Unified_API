namespace HekCoreApi.Application.Common.Interfaces;

public sealed record IdentityValidationResult(bool Succeeded, string? Subject);

/// <summary>
/// Validates a caller's credential against Microsoft Entra ID (ADR-002) - the confirmed identity
/// vendor. Each legacy system's shared service account (hsslive, ERMS/COL equivalents) is
/// registered as a real service-account credential in Entra ID, replacing the current hardcoded/
/// ad-hoc password comparison.
/// </summary>
public interface IIdentityValidator
{
    Task<IdentityValidationResult> ValidateAsync(string username, string password, CancellationToken ct = default);
}
