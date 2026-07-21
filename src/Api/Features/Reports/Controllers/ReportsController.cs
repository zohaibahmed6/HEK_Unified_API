using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Reports.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Reports;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Reports.Controllers;

[Route("patients/{patientId:int}/encounters/{encounterId:int}")]
// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class ReportsController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("lab-results")]
    public Task<IActionResult> GetLabResults(int patientId, int encounterId, [FromQuery] DateOnly? sinceDate, [FromQuery] DateOnly? untilDate, [FromQuery] string? sortOrder, CancellationToken ct) =>
        GetList(ReportKind.Lab, patientId, encounterId, sinceDate, untilDate, sortOrder, ct);

    [HttpGet("lab-results/{reportId}")]
    public Task<IActionResult> GetLabResultDetail(int patientId, int encounterId, string reportId, CancellationToken ct) =>
        GetDetail(ReportKind.Lab, patientId, reportId, ct);

    [HttpGet("radiology-results")]
    public Task<IActionResult> GetRadiologyResults(int patientId, int encounterId, [FromQuery] DateOnly? sinceDate, [FromQuery] DateOnly? untilDate, [FromQuery] string? sortOrder, CancellationToken ct) =>
        GetList(ReportKind.Radiology, patientId, encounterId, sinceDate, untilDate, sortOrder, ct);

    [HttpGet("radiology-results/{reportId}")]
    public Task<IActionResult> GetRadiologyResultDetail(int patientId, int encounterId, string reportId, CancellationToken ct) =>
        GetDetail(ReportKind.Radiology, patientId, reportId, ct);

    private async Task<IActionResult> GetList(ReportKind kind, int patientId, int encounterId, DateOnly? sinceDate, DateOnly? untilDate, string? sortOrder, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var session = new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId);
        var reports = await _mediator.Send(new GetReportListQuery(kind, CurrentScope.OriginScope, patientId, encounterId, session, sinceDate, untilDate, sortOrder), ct);
        return Ok(new ListResponse<ReportSummary>(reports));
    }

    private async Task<IActionResult> GetDetail(ReportKind kind, int patientId, string reportId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var result = await _mediator.Send(new GetReportDetailQuery(kind, CurrentScope.PracticeId, reportId), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

