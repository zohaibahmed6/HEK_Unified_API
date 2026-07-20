using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Conditions.Commands;
using HekCoreApi.Application.Features.Conditions.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Conditions;
using HekCoreApi.Contracts.Idempotency;
using HekCoreApi.Contracts.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Conditions.Controllers;

[Route("patients/{patientId:int}/encounters/{encounterId:int}/conditions")]
public sealed class ConditionsController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public ConditionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int patientId, int encounterId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var session = new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId);
        var conditions = await _mediator.Send(new GetConditionsQuery(CurrentScope.OriginScope, patientId, encounterId, session), ct);
        return Ok(new ListResponse<Condition>(conditions));
    }

    [HttpPost]
    public async Task<IActionResult> Save(int patientId, int encounterId, [FromBody] ConditionInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var idempotencyKey = Request.Headers.TryGetValue(IdempotencyHeaderNames.IdempotencyKey, out var value) ? value.ToString() : null;
        var outcome = await _mediator.Send(new SaveConditionCommand(patientId, encounterId, CurrentScope.PracticeId, input, idempotencyKey), ct);

        // Contract Design doc Section 12: idempotent duplicate -> 200 with the existing resource, not an error.
        return outcome.WasDuplicate
            ? Ok(outcome.Resource)
            : CreatedAtAction(nameof(Get), new { patientId, encounterId }, outcome.Resource);
    }
}
