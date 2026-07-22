using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.ClinicalNotes;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Fourth canonical resource (2026-07-23), same FR-2..FR-6 pattern as <see cref="CanonicalConditionsController"/>.
/// KARO/ERMS share the identical real procedure (`[HSS].[uspGetConsultNotes]`), so their allowed fields
/// match; ERMS additionally carries a real `ReferenceId` (pipe-delimited on the wire, cleaned in the
/// repository) that KARO's typed model doesn't expose, so it's correctly absent for Karo-scoped tokens.
/// </summary>
[Route("v1/patients/{patientId:int}/clinicalnotes")]
public sealed class CanonicalClinicalNotesController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(ClinicalNoteCanonical.SubjectiveNotes), nameof(ClinicalNoteCanonical.ObjectiveNotes), nameof(ClinicalNoteCanonical.Assessment), nameof(ClinicalNoteCanonical.Plans), nameof(ClinicalNoteCanonical.Date) },
            [OriginScope.Karo] = new[] { nameof(ClinicalNoteCanonical.SubjectiveNotes), nameof(ClinicalNoteCanonical.ObjectiveNotes), nameof(ClinicalNoteCanonical.Assessment), nameof(ClinicalNoteCanonical.Plans), nameof(ClinicalNoteCanonical.AppointmentAdvice), nameof(ClinicalNoteCanonical.Date) },
            [OriginScope.Erms] = new[] { nameof(ClinicalNoteCanonical.ReferenceId), nameof(ClinicalNoteCanonical.SubjectiveNotes), nameof(ClinicalNoteCanonical.ObjectiveNotes), nameof(ClinicalNoteCanonical.Assessment), nameof(ClinicalNoteCanonical.Plans), nameof(ClinicalNoteCanonical.AppointmentAdvice), nameof(ClinicalNoteCanonical.Date) }
        };

    private readonly ICanonicalClinicalNotesRepository _repository;
    private readonly ILogger<CanonicalClinicalNotesController> _logger;

    public CanonicalClinicalNotesController(ICanonicalClinicalNotesRepository repository, ILogger<CanonicalClinicalNotesController> logger)
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
                detail: $"Clinical notes are not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var notes = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = notes.Select(n => FieldSelector.Project(n, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalClinicalNotesAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<ClinicalNoteCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
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
