namespace HekCoreApi.Contracts.RateLimiting;

/// <summary>
/// Standard rate-limit response header names (Contract Design doc Section 14 / OpenAPI spec).
/// Numeric thresholds are not confirmed by any source document - see RateLimitOptions for the
/// config-driven placeholder values used until real thresholds are set (PROJECT_STATUS.md open item 18).
/// </summary>
public static class RateLimitHeaderNames
{
    public const string Limit = "X-RateLimit-Limit";
    public const string Remaining = "X-RateLimit-Remaining";
    public const string Reset = "X-RateLimit-Reset";
    public const string RetryAfter = "Retry-After";
}
