using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.EncounterSummary.Commands;
using HekCoreApi.Application.Features.EncounterSummary.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.EncounterSummary.Controllers;

public sealed class EncounterSummaryController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public EncounterSummaryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("encounter-summary-templates/{identifier}/schema")]
    public async Task<IActionResult> GetSchema(string identifier, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTemplateSchemaQuery(CurrentScope.PracticeId, identifier), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("patients/{patientId:int}/encounters/{encounterId:int}/encounter-summary")]
    public async Task<IActionResult> GetSummary(int patientId, int encounterId, [FromQuery] string identifier, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var result = await _mediator.Send(new GetEncounterSummaryQuery(patientId, encounterId, CurrentScope.PracticeId, identifier), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("patients/{patientId:int}/encounters/{encounterId:int}/encounter-summary")]
    public async Task<IActionResult> SaveSummary(int patientId, int encounterId, [FromBody] HekCoreApi.Contracts.EncounterSummary.EncounterSummaryInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var result = await _mediator.Send(new SaveEncounterSummaryCommand(patientId, encounterId, CurrentScope.PracticeId, input), ct);
        return CreatedAtAction(nameof(GetSummary), new { patientId, encounterId, identifier = result.Identifier }, result);
    }
}
