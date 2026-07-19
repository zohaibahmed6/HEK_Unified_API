namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Key-vault-shaped secret access. Every connection string/credential resolves through this
/// interface at runtime - never a literal in code or appsettings.json. Directly replaces the
/// hardcoded SQL symmetric-key password ('tcpepms*1') shared across HISO/KARO/ERMS
/// (SRS Sections 12.7, 12.10). The env-var-backed Block 0/1 implementation is swappable later for
/// Azure Key Vault or similar with zero caller-side change.
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Resolves a named secret. Throws <see cref="SecretNotFoundException"/> if <paramref name="key"/>
    /// is required but missing - never returns null/empty silently for a required secret.
    /// </summary>
    Task<string> GetRequiredSecretAsync(string key, CancellationToken ct = default);

    /// <summary>Resolves a named secret, or null if it is not configured.</summary>
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
}
