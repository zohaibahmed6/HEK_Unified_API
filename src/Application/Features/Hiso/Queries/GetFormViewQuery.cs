using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;

namespace HekCoreApi.Application.Features.Hiso.Queries;

public sealed record GetFormViewQuery(Guid SessionKey, string CalledServerAddress) : IRequest<GetFormViewQueryResult>;

public sealed record GetFormViewQueryResult(bool SessionResolved, string? ResumePath, string? ViewType, string? View, CallRoutingInfo? Routing = null);

/// <summary>
/// Ported from `Acc45DefinitionBuilder.GetACC45Definition`: `resumePath`/`viewType` are real, DB-backed.
/// The `view` bytes come via `IDmsProxyService` (real direct-DB implementation as of 2026-07-31,
/// see that interface's doc comment) - null only when the document genuinely isn't present in the
/// resolved practice's DMS database, matching a graceful "no view content" outcome rather than
/// throwing.
/// </summary>
public sealed class GetFormViewQueryHandler : IRequestHandler<GetFormViewQuery, GetFormViewQueryResult>
{
    private readonly IMediator _mediator;
    private readonly IAcc45DefinitionRepository _definitionRepository;
    private readonly IDmsProxyService _dmsProxy;
    private readonly IHisoSessionRegistryRepository _sessionRegistry;

    public GetFormViewQueryHandler(IMediator mediator, IAcc45DefinitionRepository definitionRepository, IDmsProxyService dmsProxy, IHisoSessionRegistryRepository sessionRegistry)
    {
        _mediator = mediator;
        _definitionRepository = definitionRepository;
        _dmsProxy = dmsProxy;
        _sessionRegistry = sessionRegistry;
    }

    public async Task<GetFormViewQueryResult> Handle(GetFormViewQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        if (lookup.Status != HisoSessionLookupStatus.Success || lookup.Context is null)
        {
            return new GetFormViewQueryResult(false, null, null, null);
        }

        var sessionRoute = await _sessionRegistry.FindAsync(request.SessionKey, cancellationToken);
        var routing = sessionRoute is not null ? CallRoutingInfo.FromHisoSessionRoute(sessionRoute) : null;

        var session = Application.Common.Models.HealthLinkSession.FromSessionContext(lookup.Context, request.SessionKey);
        var definition = await _definitionRepository.GetDefinitionAsync(session, cancellationToken);
        if (definition is null)
        {
            return new GetFormViewQueryResult(true, null, null, null, routing);
        }

        string? view = null;
        if (definition.DmsDocumentId is { } dmsGuid)
        {
            var bytes = await _dmsProxy.GetDocumentDataAsync(dmsGuid, session.PracticeId, cancellationToken);
            if (bytes is not null)
            {
                view = definition.ViewType?.EndsWith("html", StringComparison.OrdinalIgnoreCase) == true
                    ? System.Text.Encoding.UTF8.GetString(bytes)
                    : Convert.ToBase64String(bytes);
            }
        }

        return new GetFormViewQueryResult(true, definition.ResumePath, definition.ViewType, view, routing);
    }
}
