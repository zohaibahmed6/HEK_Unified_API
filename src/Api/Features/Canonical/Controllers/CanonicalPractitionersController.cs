using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Providers;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Ninth canonical resource (2026-07-23), same FR-2..FR-6 pattern as the others. KARO has no real
/// registered-practitioners operation (confirmed gap) - Karo-scoped tokens get a clean 501.
/// </summary>
[Route("v1/patients/{patientId:int}/practitioners")]
public sealed class CanonicalPractitionersController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(PractitionerCanonical.FullName), nameof(PractitionerCanonical.RegisteringBody), nameof(PractitionerCanonical.RegistrationNumber), nameof(PractitionerCanonical.Email) },
            [OriginScope.Erms] = new[] { nameof(PractitionerCanonical.ReferenceId), nameof(PractitionerCanonical.FullName), nameof(PractitionerCanonical.RegisteringBody), nameof(PractitionerCanonical.RegistrationNumber), nameof(PractitionerCanonical.Email) }
        };

    private readonly ICanonicalPractitionersRepository _repository;
    private readonly ILogger<CanonicalPractitionersController> _logger;

    public CanonicalPractitionersController(ICanonicalPractitionersRepository repository, ILogger<CanonicalPractitionersController> logger)
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
                detail: $"Practitioners are not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var practitioners = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = practitioners.Select(p => FieldSelector.Project(p, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalPractitionersAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<PractitionerCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
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
