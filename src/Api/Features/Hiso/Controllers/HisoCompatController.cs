using HekCoreApi.Adapters.Hiso.GetData;
using HekCoreApi.Adapters.Hiso.GetDeliveryOptions;
using HekCoreApi.Adapters.Hiso.GetFormView;
using HekCoreApi.Adapters.Hiso.GetVersion;
using HekCoreApi.Adapters.Hiso.ProcessAction;
using HekCoreApi.Adapters.Hiso.SaveContainer;
using HekCoreApi.Application.Features.Hiso.Commands;
using HekCoreApi.Application.Features.Hiso.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Hiso.Controllers;

/// <summary>
/// HISO's real `getData` SOAP operation (`FormSessionService.getData`, `HISO_doc.md` Section 3),
/// exposed here with the same field names/request-response shape - no separate bearer token
/// required, since the legacy operation authenticates via the session GUID alone ("Session GUID
/// only", `EndpointInventory.md`). Path is `/hiso/getData` (not namespaced/renamed further),
/// matching the legacy SOAP action `.../getData` as closely as an HTTP/JSON binding allows.
/// PROJECT_STATUS.md open item 34 (new, 2026-07-20): built after Zohaib flagged that renaming
/// data-retrieval operations away from their legacy names/shapes broke the ability to test against
/// an existing getData-configured consumer - this is the first of the three legacy systems' data
/// operations given real wire-compatible treatment, not just auth.
/// </summary>
[ApiController]
[Route("hiso")]
public sealed class HisoCompatController : ControllerBase
{
    private readonly IMediator _mediator;

    public HisoCompatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("getData")]
    public async Task<IActionResult> GetData([FromBody] GetDataRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value;
        var result = await _mediator.Send(
            new GetDataQuery(request.SessionKey, calledServerAddress, request.DataContainer.FormMetaData.FormInstanceOperationMode, request.DataContainer.SubmittedDataXml),
            ct);

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
        var calledServerAddress = HttpContext.Request.Host.Value;
        var sessionResolved = await _mediator.Send(new GetVersionQuery(request.SessionKey, calledServerAddress), ct);

        return sessionResolved ? Ok(GetVersionResponse.Real()) : Unauthorized(new { message = "Invalid Session Key" });
    }

    [HttpPost("getDeliveryOptions")]
    public async Task<IActionResult> GetDeliveryOptions([FromBody] GetDeliveryOptionsRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value;
        var result = await _mediator.Send(new GetDeliveryOptionsQuery(request.SessionKey, calledServerAddress), ct);

        if (!result.SessionResolved)
        {
            return Unauthorized(new { message = "Invalid Session Key" });
        }

        return Ok(GetDeliveryOptionsResponse.From(result.Url, result.SenderAccount, result.SenderPassword));
    }

    [HttpPost("processAction")]
    public async Task<IActionResult> ProcessAction([FromBody] ProcessActionRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value;
        var result = await _mediator.Send(new ProcessActionCommand(request.SessionKey, calledServerAddress, request.ActionId, request.ActionContainer, request.ActionContainerXml), ct);

        return result.SessionResolved
            ? Ok(ProcessActionResponse.From(result.Processed))
            : Unauthorized(new { message = "Invalid Session Key" });
    }

    [HttpPost("saveContainer")]
    public async Task<IActionResult> SaveContainer([FromBody] SaveContainerRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value;
        var metaData = new SaveContainerFormMetaDataInput(
            request.FormMetaData.FormInstanceId, request.FormMetaData.FormInstanceVersion, request.FormMetaData.FormEngineId,
            request.FormMetaData.FormInstanceOperationMode, request.FormMetaData.FormDefinitionId,
            request.FormMetaData.FormDefinitionVersion, request.FormMetaData.FormDefinitionTitle);

        var result = await _mediator.Send(
            new SaveContainerCommand(request.SessionKey, calledServerAddress, metaData, request.ResumePath, request.View, request.ViewType, request.ViewSignature, request.Completed, request.SubmittedDataXml),
            ct);

        return result.SessionResolved
            ? Ok(SaveContainerResponse.From(result.Response))
            : Unauthorized(new { message = "Invalid Session Key" });
    }

    [HttpPost("getFormView")]
    public async Task<IActionResult> GetFormView([FromBody] GetFormViewRequest request, CancellationToken ct)
    {
        var calledServerAddress = HttpContext.Request.Host.Value;
        var result = await _mediator.Send(new GetFormViewQuery(request.SessionKey, calledServerAddress), ct);

        return result.SessionResolved
            ? Ok(GetFormViewResponse.From(result.ResumePath, result.ViewType, result.View))
            : Unauthorized(new { message = "Invalid Session Key" });
    }
}
