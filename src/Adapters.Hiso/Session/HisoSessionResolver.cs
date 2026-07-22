using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Features.Auth.Hiso;
using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HekCoreApi.Adapters.Hiso.Session;

public sealed record HisoSessionResolutionResult(bool Succeeded, TokenResponse? Token);

/// <summary>
/// HISO's only entry point (ADR-004). Resolves a SessionGUID via the central HISO session registry
/// (HekTenantRegistry.HisoSessions, 2026-07-22), structurally assigning OriginScope.Hiso - there is
/// no caller-supplied origin field anywhere in this path (ADR-003). The Host-header/address-based
/// fallback search (ADR-007) is superseded now that routing comes from the central registry, not
/// "which address was called" - the called server address is passed through only for logging/audit,
/// no longer used for routing decisions.
/// </summary>
public sealed class HisoSessionResolver
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenIssuer _tokenIssuer;
    private readonly ITenantRegistryService _tenantRegistry;
    private readonly ILogger<HisoSessionResolver> _logger;

    public HisoSessionResolver(
        IMediator mediator,
        IJwtTokenIssuer tokenIssuer,
        ITenantRegistryService tenantRegistry,
        ILogger<HisoSessionResolver> logger)
    {
        _mediator = mediator;
        _tokenIssuer = tokenIssuer;
        _tenantRegistry = tenantRegistry;
        _logger = logger;
    }

    public async Task<HisoSessionResolutionResult> ResolveAsync(Guid sessionGuid, string calledServerAddress, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ResolveHisoSessionQuery(sessionGuid, calledServerAddress), ct);

        if (result.Status != HisoSessionLookupStatus.Success || result.Context is null)
        {
            return new HisoSessionResolutionResult(false, null);
        }

        var scope = new ResourceScope(
            PatientId: result.Context.PatientId,
            EncounterId: result.Context.AppointmentId,
            PracticeId: result.Context.PracticeId,
            OriginScope: OriginScope.Hiso);

        var token = await _tokenIssuer.IssueAsync(scope, ct);
        return new HisoSessionResolutionResult(true, token);
    }
}
