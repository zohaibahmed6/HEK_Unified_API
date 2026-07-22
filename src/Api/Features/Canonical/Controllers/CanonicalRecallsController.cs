using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Recalls;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>KARO-only canonical resource (2026-07-23) - confirmed no matching HISO concept exists and ERMS has no recalls operation; other origins get a clean 501.</summary>
[Route("v1/patients/{patientId:int}/recalls")]
public sealed class CanonicalRecallsController : ResourceScopedControllerBase
{
    private static readonly string[] AllowedFields =
    {
        nameof(RecallCanonical.CategoryId), nameof(RecallCanonical.Priority), nameof(RecallCanonical.DueDate),
        nameof(RecallCanonical.Reason), nameof(RecallCanonical.Notes)
    };

    private readonly ICanonicalRecallsRepository _repository;
    private readonly ILogger<CanonicalRecallsController> _logger;

    public CanonicalRecallsController(ICanonicalRecallsRepository repository, ILogger<CanonicalRecallsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int patientId, [FromQuery] string? fields, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        if (CurrentScope.OriginScope != OriginScope.Karo)
        {
            return Problem(
                title: "Not Supported",
                detail: $"Recalls are not available for origin '{CurrentScope.OriginScope}' - KARO-only.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var routing = new RoutingContext(CurrentScope.PracticeId, CurrentScope.PracticeCode ?? RoutingContext.Unscoped, CurrentScope.Environment ?? RoutingContext.Unscoped, CurrentScope.OriginScope);
        var recalls = await _repository.GetKaroAsync(routing, CurrentScope.PatientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = recalls.Select(r => FieldSelector.Project(r, requestedFields, AllowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalRecallsAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count);

        return Ok(new { items = projections });
    }
}
