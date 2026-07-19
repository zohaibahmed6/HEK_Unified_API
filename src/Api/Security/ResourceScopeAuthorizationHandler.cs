using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Authorization;

namespace HekCoreApi.Api.Security;

/// <summary>
/// Extracts and validates the four ADR-003 claims (patientId, practiceId, originScope - encounterId
/// is optional, since not every capability is encounter-scoped) from the authenticated principal.
/// Succeeds only if the token is well-formed; does not yet compare against a specific route's
/// resource identifiers (see ResourceScopeRequirement remarks) - that per-endpoint comparison is
/// added alongside each Block 2 domain endpoint.
/// </summary>
public sealed class ResourceScopeAuthorizationHandler : AuthorizationHandler<ResourceScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceScopeRequirement requirement)
    {
        var patientId = context.User.FindFirst(HekClaimTypes.PatientId)?.Value;
        var practiceId = context.User.FindFirst(HekClaimTypes.PracticeId)?.Value;
        var originScope = context.User.FindFirst(HekClaimTypes.OriginScope)?.Value;

        if (!string.IsNullOrEmpty(practiceId)
            && !string.IsNullOrEmpty(originScope)
            && Enum.TryParse<OriginScope>(originScope, out _))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
