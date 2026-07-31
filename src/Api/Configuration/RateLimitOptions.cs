namespace HekCoreApi.Api.Configuration;

/// <summary>
/// ADR-008: rate limiting is built as a real, working capability but rolled out via the same
/// config-toggle pattern as auth - off by default, enabled once ready. Two tiers (2026-07-30, per
/// Zohaib): a tight limit on login/authenticate endpoints (brute-force/credential-stuffing guard)
/// and a looser general limit on everything else. Revisit these numbers once real production
/// traffic/monitoring data exists.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; }

    /// <summary>General limit applied to all endpoints.</summary>
    public int PermitLimit { get; set; } = 30;

    public int WindowSeconds { get; set; } = 60;

    /// <summary>Tighter limit applied to login/authenticate endpoints only (see <see cref="HekCoreApi.Api.Security.RateLimitPolicyNames.AuthStrict"/>).</summary>
    public int AuthPermitLimit { get; set; } = 10;

    public int AuthWindowSeconds { get; set; } = 60;
}
