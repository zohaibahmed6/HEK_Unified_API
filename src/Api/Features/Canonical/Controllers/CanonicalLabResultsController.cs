using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.LabResults;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Fifth canonical resource (2026-07-23), same FR-2..FR-6 pattern as <see cref="CanonicalClinicalNotesController"/>.
/// KARO/ERMS share the identical real procedure so their core fields match; ERMS additionally exposes
/// a real `ReferenceId`/`Unit`/`ReferenceRange` that KARO's typed model doesn't carry, so those are
/// correctly absent for Karo-scoped tokens. HISO's `Patient_LaboratoryReport` concept is report-level
/// only (no Value/Unit/ReferenceRange concept fields exist for it), so those are correctly absent for
/// Hiso-scoped tokens too.
/// </summary>
[Route("v1/patients/{patientId:int}/labresults")]
public sealed class CanonicalLabResultsController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(LabResultCanonical.ReferenceId), nameof(LabResultCanonical.TestName), nameof(LabResultCanonical.Subject), nameof(LabResultCanonical.Comments), nameof(LabResultCanonical.Date) },
            [OriginScope.Karo] = new[] { nameof(LabResultCanonical.TestName), nameof(LabResultCanonical.Subject), nameof(LabResultCanonical.Value), nameof(LabResultCanonical.Date) },
            [OriginScope.Erms] = new[] { nameof(LabResultCanonical.ReferenceId), nameof(LabResultCanonical.TestName), nameof(LabResultCanonical.Subject), nameof(LabResultCanonical.Value), nameof(LabResultCanonical.Unit), nameof(LabResultCanonical.ReferenceRange), nameof(LabResultCanonical.Date) }
        };

    private readonly ICanonicalLabResultsRepository _repository;
    private readonly ILogger<CanonicalLabResultsController> _logger;

    public CanonicalLabResultsController(ICanonicalLabResultsRepository repository, ILogger<CanonicalLabResultsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int patientId, [FromQuery] string? fields, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        if (!AllowedFieldsByOrigin.TryGetValue(CurrentScope.OriginScope, out var allowedFields))
        {
            return Problem(
                title: "Not Supported",
                detail: $"Lab results are not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var results = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = results.Select(r => FieldSelector.Project(r, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalLabResultsAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<LabResultCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
        CurrentScope.OriginScope switch
        {
            OriginScope.Hiso => await _repository.GetHisoAsync(
                new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId),
                ct),
            OriginScope.Karo => await _repository.GetKaroAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            OriginScope.Erms => await _repository.GetErmsAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            _ => []
        };

    private RoutingContext RoutingContextFromScope() => new(
        CurrentScope.PracticeId,
        CurrentScope.PracticeCode ?? RoutingContext.Unscoped,
        CurrentScope.Environment ?? RoutingContext.Unscoped,
        CurrentScope.OriginScope);
}
