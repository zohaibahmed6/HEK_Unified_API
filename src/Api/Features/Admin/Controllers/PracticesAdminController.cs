using HekCoreApi.Api.Security;
using HekCoreApi.Application.Features.Admin.Commands;
using HekCoreApi.Application.Features.Admin.Queries;
using HekCoreApi.Contracts.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Admin.Controllers;

/// <summary>
/// Platform-admin scope only - not patient/clinical-resource-scoped, so this does not extend
/// ResourceScopedControllerBase (admin tokens are not necessarily tied to one patient+encounter).
/// </summary>
[ApiController]
[Route("admin/practices")]
[Authorize(Policy = AuthorizationPolicyNames.PlatformAdmin)]
public sealed class PracticesAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public PracticesAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] PracticeInput input, CancellationToken ct)
    {
        var practice = await _mediator.Send(new RegisterPracticeCommand(input), ct);
        return CreatedAtAction(nameof(Get), new { practiceId = practice.PracticeId }, practice);
    }

    [HttpGet("{practiceId}")]
    public async Task<IActionResult> Get(string practiceId, CancellationToken ct)
    {
        var practice = await _mediator.Send(new GetPracticeQuery(practiceId), ct);
        return practice is null ? NotFound() : Ok(practice);
    }

    [HttpPatch("{practiceId}")]
    public async Task<IActionResult> Update(string practiceId, [FromBody] PracticeInput input, CancellationToken ct)
    {
        var practice = await _mediator.Send(new UpdatePracticeCommand(practiceId, input), ct);
        return practice is null ? NotFound() : Ok(practice);
    }
}
