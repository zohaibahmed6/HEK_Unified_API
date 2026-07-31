namespace HekCoreApi.Api.Security;

/// <summary>Named rate-limit policy constants - never magic strings at [EnableRateLimiting] call sites.</summary>
public static class RateLimitPolicyNames
{
    /// <summary>Tight limit for login/authenticate endpoints - guards against brute-force/credential-stuffing.</summary>
    public const string AuthStrict = "auth-strict";
}
