using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Application.Features.Hiso.Queries;

/// <summary>
/// <paramref name="FormInstanceOperationMode"/>/<paramref name="SubmittedDataXml"/> carry only what
/// this handler needs from the request's `FormMetaData`/`submittedData` - the Api-layer controller
/// echoes the rest of the request's `formMetaData` back into the response unchanged (matching legacy
/// exactly), so it doesn't need to round-trip through the Application layer at all.
/// </summary>
public sealed record GetDataQuery(Guid SessionKey, string CalledServerAddress, string? FormInstanceOperationMode, string? SubmittedDataXml)
    : IRequest<GetDataQueryResult>;

public sealed record GetDataQueryResult(bool SessionResolved, string? FilledSubmittedDataXml);

/// <summary>
/// The real `getData` "dynamic mode" pipeline, ported from `legacy-reference/Hiso/FormSessionService.svc.cs`'s
/// `getData` method: resolve session -> (only when `IsDynamic` and `formInstanceOperationMode=="N"`,
/// exactly like legacy - every other combination is a genuine empty stub in legacy too, not a gap
/// this project introduced) load the real concept dictionary, parse the request XML, resolve each
/// field's backing procedure, execute them, and fill the XML template with real results.
/// </summary>
public sealed class GetDataQueryHandler : IRequestHandler<GetDataQuery, GetDataQueryResult>
{
    private readonly IMediator _mediator;
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly IHisoConceptDictionary _conceptDictionary;
    private readonly IHisoRequestEngine _requestEngine;
    private readonly HisoGetDataOptions _options;
    private readonly ILogger<GetDataQueryHandler> _logger;

    public GetDataQueryHandler(
        IMediator mediator,
        IHisoConceptExecutor hisoExecutor,
        IHisoConceptDictionary conceptDictionary,
        IHisoRequestEngine requestEngine,
        IOptions<HisoGetDataOptions> options,
        ILogger<GetDataQueryHandler> logger)
    {
        _mediator = mediator;
        _hisoExecutor = hisoExecutor;
        _conceptDictionary = conceptDictionary;
        _requestEngine = requestEngine;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GetDataQueryResult> Handle(GetDataQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        _logger.LogDebug("GetData: session lookup returned {Status}", lookup.Status);
        if (lookup.Status != HisoSessionLookupStatus.Success || lookup.Context is null)
        {
            return new GetDataQueryResult(false, null);
        }

        // Legacy: only IsDynamic=="1" AND formInstanceOperationMode=="N" has real logic - every other
        // combination is a genuine empty stub in legacy too (static mode; parked/resume mode).
        if (!_options.IsDynamic || request.FormInstanceOperationMode != "N" || string.IsNullOrWhiteSpace(request.SubmittedDataXml))
        {
            _logger.LogDebug(
                "GetData: static/non-dynamic branch, returning stub response (IsDynamic={IsDynamic}, Mode={Mode})",
                _options.IsDynamic, request.FormInstanceOperationMode);
            return new GetDataQueryResult(true, null);
        }

        var session = Application.Common.Models.HealthLinkSession.FromSessionContext(lookup.Context);

        var xDoc = new XmlDocument();
        xDoc.LoadXml(request.SubmittedDataXml);

        if (_options.AddDmsRef)
        {
            StampEmptyDmsReferenceIds(xDoc);
        }

        var concepts = await _conceptDictionary.GetConceptsAsync(session.PracticeId, cancellationToken);
        var parsedRequests = _requestEngine.ParseRequest(xDoc);
        var (preparedRequests, procedureNames) = _requestEngine.PrepareConcepts(xDoc, concepts, parsedRequests, request.FormInstanceOperationMode);

        var procedureResults = new List<Application.Common.Models.ProcedureResult>();
        foreach (var procedureName in procedureNames)
        {
            var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, session, preparedRequests, cancellationToken);
            procedureResults.Add(new Application.Common.Models.ProcedureResult { ProcedureName = procedureName, DsResult = dataSet });
        }

        await _requestEngine.FillXmlDetailsAsync(procedureResults, xDoc, concepts, preparedRequests, cancellationToken);

        return new GetDataQueryResult(true, xDoc.OuterXml);
    }

    /// <summary>Legacy: `addDMSRef` branch - stamps an empty `referenceID` onto diagnosticReport/scannedDocument group nodes missing one.</summary>
    private static void StampEmptyDmsReferenceIds(XmlDocument doc)
    {
        var groups = doc.DocumentElement?.GetElementsByTagName("group");
        if (groups is null)
        {
            return;
        }

        foreach (XmlNode group in groups)
        {
            var name = group.Attributes?["name"]?.Value;
            if (name is "clinical.diagnosticReport" or "scannedDocument" && group.Attributes?["referenceID"] is null)
            {
                var attr = doc.CreateAttribute("referenceID");
                attr.Value = string.Empty;
                group.Attributes!.Append(attr);
            }
        }
    }
}
