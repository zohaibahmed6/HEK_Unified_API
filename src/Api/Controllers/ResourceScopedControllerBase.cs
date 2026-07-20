using HekCoreApi.Api.Security;
using HekCoreApi.Contracts.Security;
using HekCoreApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Controllers;

/// <summary>
/// Base for every Block 2 domain controller. Applies the ResourceScoped policy (Block 1 - validates
/// the token carries well-formed patientId/practiceId/originScope claims) and adds the two
/// per-endpoint checks the Block 1 plan explicitly deferred to Block 2: does this token's
/// patientId match the resource being requested, and is this token's originScope allowed to reach
/// this capability at all (ADR-003 - "a KARO-origin token cannot reach ACC45-only or COL-only
/// endpoints, and vice versa").
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.ResourceScoped)]
public abstract class ResourceScopedControllerBase : ControllerBase
{
    protected ResourceScope CurrentScope => User.GetResourceScope();

    /// <summary>Throws <see cref="ForbiddenException"/> if the token is not scoped to this exact patient (ADR-003).</summary>
    protected void EnsurePatientScope(int patientId)
    {
        if (CurrentScope.PatientId != patientId.ToString())
        {
            throw new ForbiddenException("Token is not scoped to the requested patient.");
        }
    }

    /// <summary>Throws <see cref="ForbiddenException"/> if the token's originScope is not one of <paramref name="allowedOrigins"/>.</summary>
    protected void EnsureOriginScope(params OriginScope[] allowedOrigins)
    {
        if (!allowedOrigins.Contains(CurrentScope.OriginScope))
        {
            throw new ForbiddenException("Token's origin scope is not permitted to access this capability.");
        }
    }
}
