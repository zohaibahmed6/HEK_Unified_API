using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;

namespace HekCoreApi.Application.Features.Hiso.Queries;

public sealed record GetDeliveryOptionsQuery(Guid SessionKey, string CalledServerAddress) : IRequest<GetDeliveryOptionsQueryResult>;

public sealed record GetDeliveryOptionsQueryResult(bool SessionResolved, string? Url, string? SenderAccount, string? SenderPassword, CallRoutingInfo? Routing = null);

/// <summary>
/// Ported from `FormSessionService.svc.cs`'s `getDeliveryOptions`: config-sourced, not DB-sourced.
/// `senderAccount` prefers the session's `PracticeEDI` when `Hiso:PracticeEdi` secret is `"1"`, else
/// falls back to the configured `Hiso:UserId` secret - exact real branching, reproduced faithfully.
/// </summary>
public sealed class GetDeliveryOptionsQueryHandler : IRequestHandler<GetDeliveryOptionsQuery, GetDeliveryOptionsQueryResult>
{
    private readonly IMediator _mediator;
    private readonly ISecretProvider _secretProvider;
    private readonly IHisoSessionRegistryRepository _sessionRegistry;

    public GetDeliveryOptionsQueryHandler(IMediator mediator, ISecretProvider secretProvider, IHisoSessionRegistryRepository sessionRegistry)
    {
        _mediator = mediator;
        _secretProvider = secretProvider;
        _sessionRegistry = sessionRegistry;
    }

    public async Task<GetDeliveryOptionsQueryResult> Handle(GetDeliveryOptionsQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        if (lookup.Status != HisoSessionLookupStatus.Success || lookup.Context is null)
        {
            return new GetDeliveryOptionsQueryResult(false, null, null, null);
        }

        var sessionRoute = await _sessionRegistry.FindAsync(request.SessionKey, cancellationToken);
        var routing = sessionRoute is not null ? CallRoutingInfo.FromHisoSessionRoute(sessionRoute) : null;

        var practiceEdiEnabled = await _secretProvider.GetSecretAsync("Hiso:PracticeEdi", cancellationToken) == "1";
        var userId = await _secretProvider.GetSecretAsync("Hiso:UserId", cancellationToken);

        var senderAccount = practiceEdiEnabled && !string.IsNullOrEmpty(lookup.Context.PracticeEdi)
            ? lookup.Context.PracticeEdi
            : userId;

        var senderPassword = await _secretProvider.GetSecretAsync("Hiso:Password", cancellationToken);
        var url = await _secretProvider.GetSecretAsync("Hiso:Url", cancellationToken);

        return new GetDeliveryOptionsQueryResult(true, url, senderAccount, senderPassword, routing);
    }
}
