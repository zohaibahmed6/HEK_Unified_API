using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Allergies;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Seventh canonical resource (2026-07-23), same FR-2..FR-6 pattern as the others. KARO has no real
/// allergies operation (confirmed gap, not an oversight) - Karo-scoped tokens get a clean 501, same
/// pattern already used for COL on Conditions.
/// </summary>
[Route("v1/patients/{patientId:int}/allergies")]
public sealed class CanonicalAllergiesController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(AllergyCanonical.Description), nameof(AllergyCanonical.Comments), nameof(AllergyCanonical.Date) },
            [OriginScope.Erms] = new[] { nameof(AllergyCanonical.ReferenceId), nameof(AllergyCanonical.Description), nameof(AllergyCanonical.Comments), nameof(AllergyCanonical.Date) }
        };

    private readonly ICanonicalAllergiesRepository _repository;
    private readonly ILogger<CanonicalAllergiesController> _logger;

    public CanonicalAllergiesController(ICanonicalAllergiesRepository repository, ILogger<CanonicalAllergiesController> logger)
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
                detail: $"Allergies are not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var allergies = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = allergies.Select(a => FieldSelector.Project(a, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalAllergiesAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<AllergyCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
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
