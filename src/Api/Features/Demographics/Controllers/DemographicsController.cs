using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Demographics.Queries;
using HekCoreApi.Contracts.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Demographics.Controllers;

/// <summary>
/// Four separate legacy-shaped demographics endpoints (OpenAPI spec; Contract Design doc Section
/// 4.2/8 Decision 6) - never merged. Each is only reachable by a token whose originScope claim
/// matches it.
/// </summary>
[Route("patients/{patientId:int}/demographics")]
public sealed class DemographicsController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public DemographicsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("hiso")]
    public async Task<IActionResult> GetHiso(int patientId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);
        EnsureOriginScope(OriginScope.Hiso);

        // FLAGGED GAP: REST-issued tokens (ADR-003 claim set) do not carry a providerId - only
        // HISO's own SessionGUID flow resolves one (Block 1's HisoSessionResolver). Left empty here.
        var session = new HealthLinkSession(
            PatientId: CurrentScope.PatientId,
            ProviderId: string.Empty,
            AppointmentId: CurrentScope.EncounterId ?? string.Empty,
            PracticeId: CurrentScope.PracticeId);

        var result = await _mediator.Send(new GetHisoDemographicsQuery(patientId, session), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("karo")]
    public async Task<IActionResult> GetKaro(int patientId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);
        EnsureOriginScope(OriginScope.Karo);

        var result = await _mediator.Send(new GetKaroDemographicsQuery(patientId, CurrentScope.PracticeId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("erms")]
    public async Task<IActionResult> GetErms(int patientId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);
        EnsureOriginScope(OriginScope.Erms);

        var result = await _mediator.Send(new GetErmsDemographicsQuery(patientId, CurrentScope.PracticeId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("col")]
    public async Task<IActionResult> GetCol(int patientId, CancellationToken ct)
    {
        EnsurePatientScope(patientId);
        EnsureOriginScope(OriginScope.Col);

        var result = await _mediator.Send(new GetColDemographicsQuery(patientId, CurrentScope.PracticeId), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
