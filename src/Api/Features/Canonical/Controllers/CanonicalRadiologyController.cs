using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Radiology;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>Canonical resource (2026-07-23), same FR-2..FR-6 pattern as the others. KARO has no real radiology operation (confirmed gap) - Karo-scoped tokens get a clean 501.</summary>
[Route("v1/patients/{patientId:int}/radiologyreports")]
public sealed class CanonicalRadiologyController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(RadiologyReportCanonical.ReferenceId), nameof(RadiologyReportCanonical.Name), nameof(RadiologyReportCanonical.Subject), nameof(RadiologyReportCanonical.DataType), nameof(RadiologyReportCanonical.DateReceived), nameof(RadiologyReportCanonical.Comments) },
            [OriginScope.Erms] = new[] { nameof(RadiologyReportCanonical.ReferenceId), nameof(RadiologyReportCanonical.Name), nameof(RadiologyReportCanonical.Subject), nameof(RadiologyReportCanonical.DataType), nameof(RadiologyReportCanonical.DateReceived), nameof(RadiologyReportCanonical.Comments) }
        };

    private readonly ICanonicalRadiologyRepository _repository;
    private readonly ILogger<CanonicalRadiologyController> _logger;

    public CanonicalRadiologyController(ICanonicalRadiologyRepository repository, ILogger<CanonicalRadiologyController> logger)
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
                detail: $"Radiology reports are not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var reports = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = reports.Select(r => FieldSelector.Project(r, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalRadiologyAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<RadiologyReportCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
        CurrentScope.OriginScope switch
        {
            OriginScope.Hiso => await _repository.GetHisoAsync(
                new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId),
                ct),
            OriginScope.Erms => await _repository.GetErmsAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            _ => []
        };

    private RoutingContext RoutingContextFromScope() => new(
        CurrentScope.PracticeId,
        CurrentScope.PracticeCode ?? RoutingContext.Unscoped,
        CurrentScope.Environment ?? RoutingContext.Unscoped,
        CurrentScope.OriginScope);
}
