using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Api.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the ADR-003 resource-scope claims off the authenticated principal. Callers should only
    /// use this behind the <see cref="AuthorizationPolicyNames.ResourceScoped"/> policy, which
    /// already guarantees these claims are present and well-formed.
    /// </summary>
    public static ResourceScope GetResourceScope(this System.Security.Claims.ClaimsPrincipal user)
    {
        var patientId = user.FindFirst(HekClaimTypes.PatientId)?.Value ?? string.Empty;
        var encounterId = user.FindFirst(HekClaimTypes.EncounterId)?.Value;
        var practiceId = user.FindFirst(HekClaimTypes.PracticeId)?.Value ?? string.Empty;
        var practiceCode = user.FindFirst(HekClaimTypes.PracticeCode)?.Value;
        var environment = user.FindFirst(HekClaimTypes.Environment)?.Value;
        var originScope = Enum.TryParse<OriginScope>(user.FindFirst(HekClaimTypes.OriginScope)?.Value, out var scope)
            ? scope
            : throw new InvalidOperationException("Authenticated principal is missing a valid originScope claim.");

        return new ResourceScope(patientId, encounterId, practiceId, originScope, practiceCode, environment);
    }
}
