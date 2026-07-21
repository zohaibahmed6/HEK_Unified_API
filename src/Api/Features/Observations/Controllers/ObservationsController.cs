using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Observations.Commands;
using HekCoreApi.Application.Features.Observations.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Observations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Observations.Controllers;

[Route("patients/{patientId:int}/encounters/{encounterId:int}/observations")]
// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class ObservationsController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public ObservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int patientId, int encounterId, [FromQuery] string? conceptId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var session = new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId);
        var observations = await _mediator.Send(new GetObservationsQuery(CurrentScope.OriginScope, patientId, encounterId, session, conceptId), ct);
        return Ok(new ListResponse<Observation>(observations));
    }

    [HttpPost]
    public async Task<IActionResult> Save(int patientId, int encounterId, [FromBody] ObservationInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);
        EnsureOriginScope(HekCoreApi.Contracts.Security.OriginScope.Karo);

        var result = await _mediator.Send(new SaveObservationsCommand(patientId, encounterId, CurrentScope.PracticeId, input), ct);
        return CreatedAtAction(nameof(Get), new { patientId, encounterId }, result);
    }
}

