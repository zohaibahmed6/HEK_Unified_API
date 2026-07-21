using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;

namespace HekCoreApi.Application.Features.Hiso.Queries;

public sealed record GetFormViewQuery(Guid SessionKey, string CalledServerAddress) : IRequest<GetFormViewQueryResult>;

public sealed record GetFormViewQueryResult(bool SessionResolved, string? ResumePath, string? ViewType, string? View);

/// <summary>
/// Ported from `Acc45DefinitionBuilder.GetACC45Definition`: `resumePath`/`viewType` are real, DB-backed.
/// The actual `view` bytes require the external DMS Proxy (`IDmsProxyService`) - null until real
/// access is available, matching a graceful "no view content" outcome rather than throwing.
/// </summary>
public sealed class GetFormViewQueryHandler : IRequestHandler<GetFormViewQuery, GetFormViewQueryResult>
{
    private readonly IMediator _mediator;
    private readonly IAcc45DefinitionRepository _definitionRepository;
    private readonly IDmsProxyService _dmsProxy;

    public GetFormViewQueryHandler(IMediator mediator, IAcc45DefinitionRepository definitionRepository, IDmsProxyService dmsProxy)
    {
        _mediator = mediator;
        _definitionRepository = definitionRepository;
        _dmsProxy = dmsProxy;
    }

    public async Task<GetFormViewQueryResult> Handle(GetFormViewQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        if (lookup.Status != HisoSessionLookupStatus.Success || lookup.Context is null)
        {
            return new GetFormViewQueryResult(false, null, null, null);
        }

        var session = Application.Common.Models.HealthLinkSession.FromSessionContext(lookup.Context);
        var definition = await _definitionRepository.GetDefinitionAsync(session, cancellationToken);
        if (definition is null)
        {
            return new GetFormViewQueryResult(true, null, null, null);
        }

        string? view = null;
        if (definition.DmsDocumentId is { } dmsGuid)
        {
            var bytes = await _dmsProxy.GetDocumentDataAsync(dmsGuid, cancellationToken);
            if (bytes is not null)
            {
                view = definition.ViewType?.EndsWith("html", StringComparison.OrdinalIgnoreCase) == true
                    ? System.Text.Encoding.UTF8.GetString(bytes)
                    : Convert.ToBase64String(bytes);
            }
        }

        return new GetFormViewQueryResult(true, definition.ResumePath, definition.ViewType, view);
    }
}
