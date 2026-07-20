using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.Documents.Commands;
using HekCoreApi.Application.Features.Documents.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Documents;
using HekCoreApi.Contracts.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Documents.Controllers;

[Route("patients/{patientId:int}/documents")]
public sealed class DocumentsController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        int patientId, [FromQuery] string? direction, [FromQuery] string? contentType, [FromQuery] string? referenceId,
        [FromQuery] string? subject, [FromQuery] DateOnly? sinceDate, [FromQuery] DateOnly? untilDate, [FromQuery] string? sortOrder, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var documents = await _mediator.Send(new GetDocumentListQuery(CurrentScope.OriginScope, patientId, CurrentScope.PracticeId, direction, contentType, referenceId, subject, sinceDate, untilDate, sortOrder), ct);
        return Ok(new ListResponse<DocumentSummary>(documents));
    }

    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetDetail(int patientId, string documentId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var result = await _mediator.Send(new GetDocumentDetailQuery(CurrentScope.PracticeId, documentId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save(int patientId, [FromBody] DocumentInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var idempotencyKey = Request.Headers.TryGetValue(IdempotencyHeaderNames.IdempotencyKey, out var value) ? value.ToString() : null;
        var outcome = await _mediator.Send(new SaveDocumentCommand(patientId, CurrentScope.PracticeId, input, idempotencyKey), ct);

        return outcome.WasDuplicate
            ? Ok(outcome.Resource)
            : CreatedAtAction(nameof(GetDetail), new { patientId, documentId = outcome.Resource.DocumentId }, outcome.Resource);
    }
}
