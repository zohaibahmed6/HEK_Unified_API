using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Documents;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Third canonical resource (2026-07-22), same FR-2..FR-6 pattern as Demographics/Conditions. See
/// <see cref="DocumentCanonical"/> remarks for the deliberate first-pass scope (not full document
/// coverage yet - KARO `patientattachment`, ERMS discharge variant, HISO `Patient_OutgoingLetter` are
/// flagged follow-ups, not silently omitted).
/// </summary>
[Route("v1/patients/{patientId:int}/documents")]
public sealed class CanonicalDocumentsController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(DocumentCanonical.DocumentId), nameof(DocumentCanonical.Name), nameof(DocumentCanonical.Subject), nameof(DocumentCanonical.DocumentType), nameof(DocumentCanonical.DateCreated) },
            [OriginScope.Karo] = new[] { nameof(DocumentCanonical.DocumentId), nameof(DocumentCanonical.Name), nameof(DocumentCanonical.Subject), nameof(DocumentCanonical.DocumentType), nameof(DocumentCanonical.DateCreated) },
            [OriginScope.Erms] = new[] { nameof(DocumentCanonical.DocumentId), nameof(DocumentCanonical.Name), nameof(DocumentCanonical.Subject), nameof(DocumentCanonical.DocumentType), nameof(DocumentCanonical.DateCreated) }
        };

    private readonly ICanonicalDocumentsRepository _repository;
    private readonly ILogger<CanonicalDocumentsController> _logger;

    public CanonicalDocumentsController(ICanonicalDocumentsRepository repository, ILogger<CanonicalDocumentsController> logger)
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
                detail: $"Documents are not available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var documents = await FetchAsync(patientId, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = documents.Select(d => FieldSelector.Project(d, requestedFields, allowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalDocumentsAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} itemCount={ItemCount} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, projections.Count,
            projections.Count > 0 ? string.Join(",", projections[0].Keys) : string.Empty);

        return Ok(new { items = projections });
    }

    private async Task<IReadOnlyList<DocumentCanonical>> FetchAsync(int patientId, CancellationToken ct) =>
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
