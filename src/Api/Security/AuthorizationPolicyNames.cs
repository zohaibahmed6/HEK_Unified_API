namespace HekCoreApi.Api.Security;

/// <summary>Named authorization policy constants - never magic strings at [Authorize(Policy = ...)] call sites.</summary>
public static class AuthorizationPolicyNames
{
    /// <summary>Validates the token's patientId/encounterId/practiceId claims against the resource being accessed (ADR-003). Built and unit-tested in Block 1; applied per-endpoint starting in Block 2.</summary>
    public const string ResourceScoped = "ResourceScoped";

    /// <summary>Platform-admin only - reserved for /admin/practices (Block 2 domain group #16), not built in Block 1.</summary>
    public const string PlatformAdmin = "PlatformAdmin";

    /// <summary>Elevated scope required for financial writes (e.g. /patients/{id}/invoices), distinct from read-only clinical scope (SRS Section 12.2, ERMS SEC-04).</summary>
    public const string BillingWrite = "BillingWrite";
}
