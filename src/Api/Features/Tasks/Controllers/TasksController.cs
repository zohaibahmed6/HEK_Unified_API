using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.Tasks.Commands;
using HekCoreApi.Contracts.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Tasks.Controllers;

[Route("patients/{patientId:int}/tasks")]
// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class TasksController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(int patientId, [FromBody] TaskInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var result = await _mediator.Send(new CreateTaskCommand(patientId, CurrentScope.PracticeId, input), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

