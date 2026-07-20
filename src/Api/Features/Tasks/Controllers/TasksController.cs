using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.Tasks.Commands;
using HekCoreApi.Contracts.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Tasks.Controllers;

[Route("patients/{patientId:int}/tasks")]
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
