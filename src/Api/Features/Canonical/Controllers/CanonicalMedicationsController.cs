using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Medications;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Sixth canonical resource (2026-07-23), same FR-2..FR-6 pattern as <see cref="CanonicalLabResultsController"/>.
/// All three origins carry the same fields for this resource - KARO, ERMS, and HISO all resolve to the
/// same real underlying medication record shape (name/dosage/directions/startDate/isLongTerm).
/// </summary>
[Route("v1/patients/{patientId:int}/medications")]
public sealed class CanonicalMedicationsController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(MedicationCanonical.SctId), nameof(MedicationCanonical.Name), nameof(MedicationCanonical.Dosage), nameof(MedicationCanonical.Directions), nameof(MedicationCanonical.StartDate), nameof(MedicationCanonical.IsLongTerm) },
            [OriginScope.Karo] = new[] { nameof(MedicationCanonical.SctId), nameof(MedicationCanonical.Name), nameof(MedicationCanonical.Dosage), nameof(MedicationCanonical.Directions), nameof(MedicationCanonical.StartDate), nameof(MedicationCanonical.IsLongTerm) },
            [OriginScope.Erms] = new[] { nameof(MedicationCanonical.SctId), nameof(MedicationCanonical.Name), nameof(MedicationCanonical.Dosage), nameof(MedicationCanonical.Directions), nameof(MedicationCanonical.StartDate), nameof(MedicationCanonical.IsLongTerm) }
        };

    private readonly ICanonicalMedicationsRepository _repository;
    private readonly ILogger<CanonicalMedicationsController> _logger;

    public CanonicalMedicationsController(ICanonicalMedicationsRepository repository, ILogger<CanonicalMedicationsController> logger)
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
                detail: $"Medications are not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var medications = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = medications.Select(m => FieldSelector.Project(m, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalMedicationsAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<MedicationCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
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
