using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Conditions;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Second canonical resource (2026-07-22), same FR-2..FR-6 pattern as <see cref="CanonicalDemographicsController"/>:
/// one route for every consumer, reuses <see cref="ICanonicalConditionsRepository"/> (zero new DB
/// guessing), per-origin field scoping, audit-logged. KARO/ERMS share the identical real procedure so
/// their allowed fields are the same; HISO's real `Patient_Problem` concept doesn't carry a "Summary"
/// field, so it's correctly absent for Hiso-scoped tokens.
/// </summary>
[Route("v1/patients/{patientId:int}/conditions")]
public sealed class CanonicalConditionsController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(ConditionCanonical.ConceptId), nameof(ConditionCanonical.Name), nameof(ConditionCanonical.DiagnosisDate), nameof(ConditionCanonical.IsLongTerm) },
            [OriginScope.Karo] = new[] { nameof(ConditionCanonical.ConceptId), nameof(ConditionCanonical.Name), nameof(ConditionCanonical.Summary), nameof(ConditionCanonical.DiagnosisDate), nameof(ConditionCanonical.IsLongTerm) },
            [OriginScope.Erms] = new[] { nameof(ConditionCanonical.ConceptId), nameof(ConditionCanonical.Name), nameof(ConditionCanonical.Summary), nameof(ConditionCanonical.DiagnosisDate), nameof(ConditionCanonical.IsLongTerm) },
            [OriginScope.Col] = new[] { nameof(ConditionCanonical.ConceptId), nameof(ConditionCanonical.Name), nameof(ConditionCanonical.DiagnosisDate) }
        };

    private readonly ICanonicalConditionsRepository _repository;
    private readonly ILogger<CanonicalConditionsController> _logger;

    public CanonicalConditionsController(ICanonicalConditionsRepository repository, ILogger<CanonicalConditionsController> logger)
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
                detail: $"Conditions are not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var conditions = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = conditions.Select(c => FieldSelector.Project(c, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalConditionsAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<ConditionCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
        CurrentScope.OriginScope switch
        {
            OriginScope.Hiso => await _repository.GetHisoAsync(
                new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId),
                ct),
            OriginScope.Karo => await _repository.GetKaroAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            OriginScope.Erms => await _repository.GetErmsAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            OriginScope.Col => await _repository.GetColAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            _ => []
        };

    private RoutingContext RoutingContextFromScope() => new(
        CurrentScope.PracticeId,
        CurrentScope.PracticeCode ?? RoutingContext.Unscoped,
        CurrentScope.Environment ?? RoutingContext.Unscoped,
        CurrentScope.OriginScope);
}
