namespace HekCoreApi.Api.Configuration;

/// <summary>
/// ADR-008: rate limiting is built as a real, working capability but rolled out via the same
/// config-toggle pattern as auth - generous/effectively-off at launch, tightened once monitoring
/// confirms real thresholds are safe. No numeric thresholds are confirmed by any source document
/// (PROJECT_STATUS.md open item 18) - these are placeholders, not confirmed values.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; }

    public int PermitLimit { get; set; } = 10_000;

    public int WindowSeconds { get; set; } = 60;
}
