using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;

namespace HekCoreApi.Application.Features.Hiso.Queries;

public sealed record GetVersionQuery(Guid SessionKey, string CalledServerAddress) : IRequest<bool>;

/// <summary>Legacy `getVersion` only validates the session - the response payload is entirely hardcoded (see `Adapters.Hiso.GetVersion.GetVersionResponse`).</summary>
public sealed class GetVersionQueryHandler : IRequestHandler<GetVersionQuery, bool>
{
    private readonly IMediator _mediator;

    public GetVersionQueryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<bool> Handle(GetVersionQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        return lookup.Status == HisoSessionLookupStatus.Success;
    }
}
