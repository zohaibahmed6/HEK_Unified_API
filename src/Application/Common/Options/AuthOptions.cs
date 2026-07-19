namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// ADR-008: authentication itself ships as a configuration toggle, defaulted OFF, so the merged
/// platform can first be validated against legacy-equivalent behavior before being enabled. No
/// automated production hard-block exists (explicit stakeholder decision, ADR-008) - enabling this
/// in production is a manual step Zohaib owns.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool Enabled { get; set; }

    /// <summary>Signing key identifier resolved via ISecretProvider - never a literal here.</summary>
    public string SigningKeySecretName { get; set; } = "Auth:JwtSigningKey";

    public int TokenExpiryHours { get; set; } = 12;
}
