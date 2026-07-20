using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Medications.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Medications;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace HekCoreApi.Api.Features.Medications.Controllers;

[Route("patients/{patientId:int}/encounters/{encounterId:int}/medications")]
public sealed class MedicationsController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public MedicationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int patientId, int encounterId, [FromQuery] string? view, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var session = new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId);
        var medications = await _mediator.Send(new GetMedicationsQuery(CurrentScope.OriginScope, patientId, encounterId, session, view), ct);
        return Ok(new ListResponse<Medication>(medications));
    }
}
