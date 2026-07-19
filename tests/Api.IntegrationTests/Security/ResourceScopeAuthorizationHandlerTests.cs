using System.Security.Claims;
using HekCoreApi.Api.Security;
using HekCoreApi.Contracts.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace Api.IntegrationTests.Security;

/// <summary>
/// Block 1's ResourceScoped policy is built and unit-tested here but not yet applied to any
/// endpoint (Block 2 wires [Authorize(Policy = ResourceScoped)] onto real routes) - see plan.
/// </summary>
public sealed class ResourceScopeAuthorizationHandlerTests
{
    private static async Task<bool> EvaluateAsync(ClaimsPrincipal principal)
    {
        var handler = new ResourceScopeAuthorizationHandler();
        var requirement = new ResourceScopeRequirement();
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task Succeeds_WhenPracticeIdAndOriginScopeClaimsPresent()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(HekClaimTypes.PracticeId, "demo"),
            new Claim(HekClaimTypes.OriginScope, nameof(OriginScope.Karo))
        ]));

        (await EvaluateAsync(principal)).Should().BeTrue();
    }

    [Fact]
    public async Task Fails_WhenPracticeIdClaimMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(HekClaimTypes.OriginScope, nameof(OriginScope.Karo))
        ]));

        (await EvaluateAsync(principal)).Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenOriginScopeClaimIsNotAValidEnumValue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(HekClaimTypes.PracticeId, "demo"),
            new Claim(HekClaimTypes.OriginScope, "NotARealOrigin")
        ]));

        (await EvaluateAsync(principal)).Should().BeFalse();
    }
}
