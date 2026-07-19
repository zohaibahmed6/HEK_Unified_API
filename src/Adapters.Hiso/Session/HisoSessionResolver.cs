using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Application.Features.Auth.Hiso;
using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Adapters.Hiso.Session;

public sealed record HisoSessionResolutionResult(bool Succeeded, TokenResponse? Token);

/// <summary>
/// HISO's only entry point (ADR-004/ADR-007). Resolves a SessionGUID at the address the
/// HealthLink-style form engine called, structurally assigning OriginScope.Hiso - there is no
/// caller-supplied origin field anywhere in this path (ADR-003). Falls back to a search across
/// other known HISO-origin practices only when the primary address lookup misses, logging the
/// fallback as an anomaly rather than silently retrying (ADR-007).
/// </summary>
public sealed class HisoSessionResolver
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenIssuer _tokenIssuer;
    private readonly ITenantRegistryService _tenantRegistry;
    private readonly HisoServerAddressMapOptions _addressMap;
    private readonly ILogger<HisoSessionResolver> _logger;

    public HisoSessionResolver(
        IMediator mediator,
        IJwtTokenIssuer tokenIssuer,
        ITenantRegistryService tenantRegistry,
        IOptions<HisoServerAddressMapOptions> addressMap,
        ILogger<HisoSessionResolver> logger)
    {
        _mediator = mediator;
        _tokenIssuer = tokenIssuer;
        _tenantRegistry = tenantRegistry;
        _addressMap = addressMap.Value;
        _logger = logger;
    }

    public async Task<HisoSessionResolutionResult> ResolveAsync(Guid sessionGuid, string calledServerAddress, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ResolveHisoSessionQuery(sessionGuid, calledServerAddress), ct);

        if (result.Status == HisoSessionLookupStatus.NotFound)
        {
            _logger.LogWarning(
                "HISO session did not resolve on its expected address {ServerAddress} - triggering fallback search (ADR-007 anomaly).",
                calledServerAddress);

            foreach (var fallbackAddress in _addressMap.Addresses.Keys.Where(a => a != calledServerAddress))
            {
                result = await _mediator.Send(new ResolveHisoSessionQuery(sessionGuid, fallbackAddress), ct);
                if (result.Status == HisoSessionLookupStatus.Success)
                {
                    _logger.LogWarning(
                        "HISO session resolved via fallback address {FallbackAddress} instead of {ExpectedAddress} - anomaly, not the normal path.",
                        fallbackAddress,
                        calledServerAddress);
                    break;
                }
            }
        }

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
