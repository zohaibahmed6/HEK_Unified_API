using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.Recalls.Commands;
using HekCoreApi.Application.Features.Recalls.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Recalls;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Recalls.Controllers;

// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class RecallsController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public RecallsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("recall-categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string? group, CancellationToken ct)
    {
        var categories = await _mediator.Send(new GetRecallCategoriesQuery(CurrentScope.PracticeId, group), ct);
        return Ok(new ListResponse<RecallCategory>(categories));
    }

    [HttpGet("patients/{patientId:int}/recalls")]
    public async Task<IActionResult> GetForPatient(int patientId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var recalls = await _mediator.Send(new GetRecallsForPatientQuery(patientId, CurrentScope.PracticeId), ct);
        return Ok(new ListResponse<Recall>(recalls));
    }

    [HttpPost("patients/{patientId:int}/recalls")]
    public async Task<IActionResult> Save(int patientId, [FromBody] RecallInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var result = await _mediator.Send(new SaveRecallCommand(patientId, CurrentScope.PracticeId, input), ct);
        return CreatedAtAction(nameof(GetForPatient), new { patientId }, result);
    }
}

