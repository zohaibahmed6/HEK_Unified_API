using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.ClinicalNotes.Commands;
using HekCoreApi.Application.Features.ClinicalNotes.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.ClinicalNotes;
using HekCoreApi.Contracts.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.ClinicalNotes.Controllers;

[Route("patients/{patientId:int}/encounters/{encounterId:int}/notes")]
public sealed class ClinicalNotesController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public ClinicalNotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        int patientId, int encounterId, [FromQuery] DateOnly? sinceDate, [FromQuery] DateOnly? untilDate, [FromQuery] string? sortOrder, CancellationToken ct)
    {
        EnsurePatientScope(patientId);
        EnsureOriginScope(OriginScope.Hiso, OriginScope.Karo, OriginScope.Erms);

        var session = new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId);
        var notes = await _mediator.Send(new GetClinicalNotesQuery(CurrentScope.OriginScope, patientId, encounterId, session, sinceDate, untilDate, sortOrder), ct);
        return Ok(new ListResponse<ClinicalNote>(notes));
    }

    [HttpPost]
    public async Task<IActionResult> Save(int patientId, int encounterId, [FromBody] ClinicalNoteInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);
        EnsureOriginScope(OriginScope.Karo);

        var note = await _mediator.Send(new SaveClinicalNoteCommand(patientId, encounterId, CurrentScope.PracticeId, input.Content), ct);
        return CreatedAtAction(nameof(Get), new { patientId, encounterId }, note);
    }
}
