using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Security;
using HekCoreApi.Contracts.Smoking;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>Canonical resource (2026-07-23), same FR-2..FR-6 pattern as the others. KARO has no real smoking-status operation (confirmed gap) - Karo-scoped tokens get a clean 501.</summary>
[Route("v1/patients/{patientId:int}/smokingstatus")]
public sealed class CanonicalSmokingStatusController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(SmokingStatusCanonical.ConsumptionDescription) },
            [OriginScope.Erms] = new[] { nameof(SmokingStatusCanonical.ReferenceId), nameof(SmokingStatusCanonical.ConsumptionDescription), nameof(SmokingStatusCanonical.Date) }
        };

    private readonly ICanonicalSmokingStatusRepository _repository;
    private readonly ILogger<CanonicalSmokingStatusController> _logger;

    public CanonicalSmokingStatusController(ICanonicalSmokingStatusRepository repository, ILogger<CanonicalSmokingStatusController> logger)
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
                detail: $"Smoking status is not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var entries = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = entries.Select(e => FieldSelector.Project(e, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalSmokingStatusAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<SmokingStatusCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
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
