namespace HekCoreApi.Api.Configuration;

/// <summary>
/// ADR-008: rate limiting is built as a real, working capability but rolled out via the same
/// config-toggle pattern as auth - generous/effectively-off at launch, tightened once monitoring
/// confirms real thresholds are safe. No source document has quantitative traffic figures (SRS
/// §5.5/§19: "cannot be confirmed from analysis" beyond a qualitative 10,000-concurrent-users
/// target) - PROJECT_STATUS.md open item 18, resolved 2026-07-20: rather than fabricate a "final"
/// number no evidence supports, Zohaib confirmed these values ARE the deliberate Day-1 setting
/// (per the Contract Design doc's own already-approved "generous now, tighten after monitoring"
/// decision, Section 14), not an unconfirmed placeholder awaiting a real number. Revisit once real
/// production traffic/monitoring data exists to tighten safely.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; }

    public int PermitLimit { get; set; } = 10_000;

    public int WindowSeconds { get; set; } = 60;
}
