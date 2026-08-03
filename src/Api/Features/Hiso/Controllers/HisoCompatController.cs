using HekCoreApi.Adapters.Hiso.GetData;
using HekCoreApi.Adapters.Hiso.GetDeliveryOptions;
using HekCoreApi.Adapters.Hiso.GetFormView;
using HekCoreApi.Adapters.Hiso.GetVersion;
using HekCoreApi.Adapters.Hiso.ProcessAction;
using HekCoreApi.Adapters.Hiso.SaveContainer;
using HekCoreApi.Api.Telemetry;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Hiso.Commands;
using HekCoreApi.Application.Features.Hiso.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Hiso.Controllers;

/// <summary>
/// Internal-only JSON mirror of HISO's real SOAP operations (<see cref="Soap.FormSessionService"/>
/// at `/FormSessionService.svc`, which is the actual legacy-compatible door real external HISO
/// clients must use). This controller exists solely so the project's own frontend dashboard
/// (`frontend/src/hisoView.tsx`/`catalog.ts`) has a plain JSON transport to call - it is not a
/// public legacy-compat surface and should not be advertised to real external HISO clients.
/// v1.1 plan Step 4 follow-up (2026-07-27): originally flagged as a not-yet-retired duplicate
/// JSON door; scoped down to "internal dashboard API only" instead of deletion, since the
/// frontend calls these JSON routes directly and has no SOAP client of its own.
/// CORRECTION (2026-07-30): this comment previously claimed `getFormView` "has no SOAP equivalent...
/// not a real legacy operation" - that was wrong. A live WSDL fetch against the real legacy server
/// (`http://localhost:53507/FormSessionService.svc?wsdl`) during that day's crosscheck confirmed
/// `getFormView` is a genuine 6th real SOAP operation, now wired into
/// <see cref="Soap.IFormSessionService"/>/<see cref="Soap.FormSessionService"/> alongside the other 5.
/// This JSON copy stays as-is for the frontend dashboard; both transports now exist.
/// </summary>
[ApiController]
[Route("hiso")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HisoCompatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<HisoCompatController> _logger;
    private readonly LegacyOperationObserver _observer;

    public HisoCompatController(IMediator mediator, ILogger<HisoCompatController> logger, LegacyOperationObserver observer)
    {
        _mediator = mediator;
        _logger = logger;
        _observer = observer;
    }

    /// <summary>Routing is only known after the mediator call resolves the session - this enriches the
    /// CallLog readable line to reflect it too, via ObserveAsync's enrichContext hook (not just headers).</summary>
    private static Dictionary<string, object?> RoutingContext(CallRoutingInfo? routing) =>
        routing is { } r
            ? new Dictionary<string, object?> { ["Routing.DbServerHost"] = r.DbServerHost, ["Routing.Environment"] = r.Environment, ["Routing.DbName"] = r.DbName, ["Routing.PracticeId"] = r.PracticeId }
            : new Dictionary<string, object?>();

    [HttpPost("getData")]
    public async Task<IActionResult> GetData([FromBody] GetDataRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value ?? string.Empty;
        var context = new Dictionary<string, object?> { ["SessionKey"] = request.SessionKey, ["FormInstanceOperationMode"] = request.DataContainer.FormMetaData.FormInstanceOperationMode };

        var result = await _observer.ObserveAsync(
            _logger, "hiso", "getData", context,
            () => _mediator.Send(
                new GetDataQuery(request.SessionKey, calledServerAddress, request.DataContainer.FormMetaData.FormInstanceOperationMode, request.DataContainer.SubmittedDataXml),
                ct),
            isExpectedFailure: r => !r.SessionResolved,
            expectedFailureReason: "InvalidSessionKey",
            enrichContext: r => RoutingContext(r.Routing));

        Response.WriteRoutingHeaders("hiso", result.Routing);

        // Legacy: FaultException("Invalid Session Key") - reproduced as the literal message text,
        // matching the real fault string exactly rather than a generic 401.
        if (!result.SessionResolved)
        {
            return Unauthorized(new { message = "Invalid Session Key" });
        }

        // Legacy: real dynamic-mode/new-form gate didn't pass - `@return` stays null (static mode,
        // parked/resume mode are genuine empty stubs in legacy too, not gaps this project introduced).
        if (result.FilledSubmittedDataXml is null)
        {
            return Ok(GetDataResponse.Empty());
        }

        return Ok(GetDataResponse.From(request.DataContainer.FormMetaData, result.FilledSubmittedDataXml));
    }

    [HttpPost("getVersion")]
    public async Task<IActionResult> GetVersion([FromBody] GetVersionRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value ?? string.Empty;
        var context = new Dictionary<string, object?> { ["SessionKey"] = request.SessionKey };

        var result = await _observer.ObserveAsync(
            _logger, "hiso", "getVersion", context,
            () => _mediator.Send(new GetVersionQuery(request.SessionKey, calledServerAddress), ct),
            isExpectedFailure: r => !r.SessionResolved,
            expectedFailureReason: "InvalidSessionKey",
            enrichContext: r => RoutingContext(r.Routing));

        Response.WriteRoutingHeaders("hiso", result.Routing);

        return result.SessionResolved ? Ok(GetVersionResponse.Real()) : Unauthorized(new { message = "Invalid Session Key" });
    }

    [HttpPost("getDeliveryOptions")]
    public async Task<IActionResult> GetDeliveryOptions([FromBody] GetDeliveryOptionsRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value ?? string.Empty;
        var context = new Dictionary<string, object?> { ["SessionKey"] = request.SessionKey };

        var result = await _observer.ObserveAsync(
            _logger, "hiso", "getDeliveryOptions", context,
            () => _mediator.Send(new GetDeliveryOptionsQuery(request.SessionKey, calledServerAddress), ct),
            isExpectedFailure: r => !r.SessionResolved,
            expectedFailureReason: "InvalidSessionKey",
            enrichContext: r => RoutingContext(r.Routing));

        Response.WriteRoutingHeaders("hiso", result.Routing);

        if (!result.SessionResolved)
        {
            return Unauthorized(new { message = "Invalid Session Key" });
        }

        return Ok(GetDeliveryOptionsResponse.From(result.Url, result.SenderAccount, result.SenderPassword));
    }

    [HttpPost("processAction")]
    public async Task<IActionResult> ProcessAction([FromBody] ProcessActionRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value ?? string.Empty;
        var context = new Dictionary<string, object?> { ["SessionKey"] = request.SessionKey, ["ActionId"] = request.ActionId };

        var result = await _observer.ObserveAsync(
            _logger, "hiso", "processAction", context,
            () => _mediator.Send(new ProcessActionCommand(request.SessionKey, calledServerAddress, request.ActionId, request.ActionContainer, request.ActionContainerXml), ct),
            isExpectedFailure: r => !r.SessionResolved,
            expectedFailureReason: "InvalidSessionKey",
            enrichContext: r => RoutingContext(r.Routing));

        Response.WriteRoutingHeaders("hiso", result.Routing);

        return result.SessionResolved
            ? Ok(ProcessActionResponse.From(result.Processed))
            : Unauthorized(new { message = "Invalid Session Key" });
    }

    [HttpPost("saveContainer")]
    public async Task<IActionResult> SaveContainer([FromBody] SaveContainerRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value ?? string.Empty;
        var metaData = new SaveContainerFormMetaDataInput(
            request.FormMetaData.FormInstanceId, request.FormMetaData.FormInstanceVersion, request.FormMetaData.FormEngineId,
            request.FormMetaData.FormInstanceOperationMode, request.FormMetaData.FormDefinitionId,
            request.FormMetaData.FormDefinitionVersion, request.FormMetaData.FormDefinitionTitle);
        var context = new Dictionary<string, object?> { ["SessionKey"] = request.SessionKey, ["FormInstanceId"] = request.FormMetaData.FormInstanceId };

        var result = await _observer.ObserveAsync(
            _logger, "hiso", "saveContainer", context,
            () => _mediator.Send(
                new SaveContainerCommand(request.SessionKey, calledServerAddress, metaData, request.ResumePath, request.View, request.ViewType, request.ViewSignature, request.Completed, request.SubmittedDataXml),
                ct),
            isExpectedFailure: r => !r.SessionResolved,
            expectedFailureReason: "InvalidSessionKey",
            enrichContext: r => RoutingContext(r.Routing));

        Response.WriteRoutingHeaders("hiso", result.Routing);

        return result.SessionResolved
            ? Ok(SaveContainerResponse.From(result.Response))
            : Unauthorized(new { message = "Invalid Session Key" });
    }

    [HttpPost("getFormView")]
    public async Task<IActionResult> GetFormView([FromBody] GetFormViewRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value ?? string.Empty;
        var context = new Dictionary<string, object?> { ["SessionKey"] = request.SessionKey };

        var result = await _observer.ObserveAsync(
            _logger, "hiso", "getFormView", context,
            () => _mediator.Send(new GetFormViewQuery(request.SessionKey, calledServerAddress), ct),
            isExpectedFailure: r => !r.SessionResolved,
            expectedFailureReason: "InvalidSessionKey",
            enrichContext: r => RoutingContext(r.Routing));

        Response.WriteRoutingHeaders("hiso", result.Routing);

        return result.SessionResolved
            ? Ok(GetFormViewResponse.From(result.ResumePath, result.ViewType, result.View))
            : Unauthorized(new { message = "Invalid Session Key" });
    }
}
