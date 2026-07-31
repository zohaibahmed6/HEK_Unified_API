using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;

namespace HekCoreApi.Application.Features.Hiso.Queries;

public sealed record GetVersionQuery(Guid SessionKey, string CalledServerAddress) : IRequest<GetVersionQueryResult>;

public sealed record GetVersionQueryResult(bool SessionResolved, CallRoutingInfo? Routing = null);

/// <summary>Legacy `getVersion` only validates the session - the response payload is entirely hardcoded (see `Adapters.Hiso.GetVersion.GetVersionResponse`).</summary>
public sealed class GetVersionQueryHandler : IRequestHandler<GetVersionQuery, GetVersionQueryResult>
{
    private readonly IMediator _mediator;
    private readonly IHisoSessionRegistryRepository _sessionRegistry;

    public GetVersionQueryHandler(IMediator mediator, IHisoSessionRegistryRepository sessionRegistry)
    {
        _mediator = mediator;
        _sessionRegistry = sessionRegistry;
    }

    public async Task<GetVersionQueryResult> Handle(GetVersionQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        if (lookup.Status != HisoSessionLookupStatus.Success)
        {
            return new GetVersionQueryResult(false);
        }

        var sessionRoute = await _sessionRegistry.FindAsync(request.SessionKey, cancellationToken);
        var routing = sessionRoute is not null ? CallRoutingInfo.FromHisoSessionRoute(sessionRoute) : null;
        return new GetVersionQueryResult(true, routing);
    }
}
