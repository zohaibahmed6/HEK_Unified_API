using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Acc45.Commands;
using HekCoreApi.Application.Features.Acc45.Queries;
using HekCoreApi.Contracts.Acc45;
using HekCoreApi.Contracts.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Acc45.Controllers;

/// <summary>
/// Canonical internal REST equivalents of HISO's FormSessionService.svc SOAP contract (Contract
/// Design doc Section 4.9) - not directly exposed to HISO's existing SOAP consumer (Contract
/// Design doc explicit note). Gated to HISO-origin tokens only; sessionKey is a resource
/// identifier, not the auth mechanism (the caller already holds a token).
/// </summary>
[Route("acc45")]
public sealed class Acc45Controller : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public Acc45Controller(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("version")]
    public async Task<IActionResult> GetVersion(CancellationToken ct)
    {
        EnsureOriginScope(OriginScope.Hiso);
        return Ok(await _mediator.Send(new GetAcc45VersionQuery(), ct));
    }

    [HttpGet("sessions/{sessionKey:guid}")]
    public async Task<IActionResult> GetSession(Guid sessionKey, CancellationToken ct)
    {
        EnsureOriginScope(OriginScope.Hiso);
        var result = await _mediator.Send(new GetAcc45SessionQuery(sessionKey, CurrentScope.PracticeId), ct);
        return Ok(result);
    }

    [HttpGet("sessions/{sessionKey:guid}/delivery-options")]
    public async Task<IActionResult> GetDeliveryOptions(Guid sessionKey, CancellationToken ct)
    {
        EnsureOriginScope(OriginScope.Hiso);
        var session = CurrentHisoSession();
        return Ok(await _mediator.Send(new GetAcc45DeliveryOptionsQuery(session), ct));
    }

    [HttpGet("sessions/{sessionKey:guid}/forms")]
    public async Task<IActionResult> GetForms(Guid sessionKey, [FromQuery] string mode = "dynamic", CancellationToken ct = default)
    {
        EnsureOriginScope(OriginScope.Hiso);
        var session = CurrentHisoSession();
        return Ok(await _mediator.Send(new GetAcc45FormsQuery(sessionKey, session, mode), ct));
    }

    [HttpGet("sessions/{sessionKey:guid}/forms/{formInstanceId}")]
    public async Task<IActionResult> GetForm(Guid sessionKey, string formInstanceId, CancellationToken ct)
    {
        EnsureOriginScope(OriginScope.Hiso);
        var session = CurrentHisoSession();
        var result = await _mediator.Send(new GetAcc45FormQuery(sessionKey, formInstanceId, session), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("sessions/{sessionKey:guid}/forms/{formInstanceId}")]
    public async Task<IActionResult> SaveForm(Guid sessionKey, string formInstanceId, [FromBody] FormSaveInput input, CancellationToken ct)
    {
        EnsureOriginScope(OriginScope.Hiso);
        var session = CurrentHisoSession();
        return Ok(await _mediator.Send(new SaveAcc45FormCommand(sessionKey, formInstanceId, session, input), ct));
    }

    [HttpPost("sessions/{sessionKey:guid}/actions")]
    public async Task<IActionResult> DispatchAction(Guid sessionKey, [FromBody] FormActionInput input, CancellationToken ct)
    {
        EnsureOriginScope(OriginScope.Hiso);
        var session = CurrentHisoSession();
        return Ok(await _mediator.Send(new DispatchAcc45ActionCommand(sessionKey, session, input), ct));
    }

    private HealthLinkSession CurrentHisoSession() =>
        new(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId);
}
