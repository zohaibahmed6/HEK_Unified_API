using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.Screening.Commands;
using HekCoreApi.Application.Features.Screening.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Screening;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Screening.Controllers;

// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class ScreeningController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public ScreeningController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("screening-codes")]
    public async Task<IActionResult> GetCodes(CancellationToken ct)
    {
        var codes = await _mediator.Send(new GetScreeningCodesQuery(CurrentScope.PracticeId), ct);
        return Ok(new ListResponse<ScreeningCode>(codes));
    }

    [HttpPost("patients/{patientId:int}/encounters/{encounterId:int}/screening-codes")]
    public async Task<IActionResult> Save(int patientId, int encounterId, [FromBody] ScreeningCodeInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var result = await _mediator.Send(new SaveScreeningCodeCommand(patientId, encounterId, CurrentScope.PracticeId, input), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

