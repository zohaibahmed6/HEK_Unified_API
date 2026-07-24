using HekCoreApi.Api.Controllers;
using HekCoreApi.Api.Telemetry;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Demographics;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// HEK_UNIFIED_API_SPEC.md FR-2/FR-3/FR-4/FR-5/FR-6: one canonical demographics endpoint for every
/// consumer, regardless of which legacy system it used to call. Reuses the already-verified
/// <see cref="IDemographicsRepository"/> (zero new DB code) and maps the caller's own source-specific
/// shape onto <see cref="DemographicsCanonical"/>. A consumer's allowed fields are exactly the fields
/// its own legacy DTO exposed (FR-5: "a consumer may access only the fields defined in its own
/// standard") - so no separate scope-configuration subsystem is needed for the demo. Every call is
/// audit-logged (consumer, timestamp, endpoint, exact fields returned) via the existing correlation
/// ID / Serilog pipeline (FR-6).
/// </summary>
[Route("v1/patients/{patientId:int}/demographics")]
public sealed class CanonicalDemographicsController : ResourceScopedControllerBase
{
    // Allowed fields are expressed in canonical property names (FR-5: "a consumer may access only
    // the fields defined in its own standard") - one entry per legacy DTO's fields, translated onto
    // DemographicsCanonical's naming (e.g. ERMS's "Dob" -> canonical "DateOfBirth").
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(DemographicsCanonical.PatientId), nameof(DemographicsCanonical.PracticeId), nameof(DemographicsCanonical.FirstName), nameof(DemographicsCanonical.LastName), nameof(DemographicsCanonical.DateOfBirth) },
            [OriginScope.Karo] = new[] { nameof(DemographicsCanonical.PatientId), nameof(DemographicsCanonical.PracticeId), nameof(DemographicsCanonical.FirstName), nameof(DemographicsCanonical.LastName), nameof(DemographicsCanonical.DateOfBirth), nameof(DemographicsCanonical.DateOfEnrolment), nameof(DemographicsCanonical.EndEnrolmentDate) },
            [OriginScope.Erms] = new[] { nameof(DemographicsCanonical.PatientId), nameof(DemographicsCanonical.EncounterId), nameof(DemographicsCanonical.FirstName), nameof(DemographicsCanonical.LastName), nameof(DemographicsCanonical.DateOfBirth), nameof(DemographicsCanonical.Nhi) },
            [OriginScope.Col] = new[] { nameof(DemographicsCanonical.PatientId), nameof(DemographicsCanonical.PracticeId), nameof(DemographicsCanonical.FirstName), nameof(DemographicsCanonical.LastName) }
        };

    private readonly IDemographicsRepository _repository;
    private readonly ILogger<CanonicalDemographicsController> _logger;
    private readonly HekTelemetry _telemetry;

    public CanonicalDemographicsController(IDemographicsRepository repository, ILogger<CanonicalDemographicsController> logger, HekTelemetry telemetry)
    {
        _repository = repository;
        _logger = logger;
        _telemetry = telemetry;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int patientId, [FromQuery] string? fields, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var canonical = await FetchAsync(patientId, ct);
        if (canonical is null)
        {
            return NotFound();
        }

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var allowedFields = AllowedFieldsByOrigin[CurrentScope.OriginScope];
        var projection = FieldSelector.Project(canonical, requestedFields, allowedFields);

        // FR-5/FR-6 telemetry (NFR-5): how many fields this call actually returned vs. how many the
        // caller asked for outside its own scope and got silently dropped - a live, graphable version
        // of the same field-scoping guarantee the demo proves manually in CanonicalDemoScript.md.
        var blockedCount = requestedFields is { Length: > 0 }
            ? requestedFields.Count(f => !allowedFields.Contains(f, StringComparer.OrdinalIgnoreCase))
            : 0;
        _telemetry.RecordFieldScoping(CurrentScope.OriginScope.ToString(), Request.Path, projection.Count, blockedCount);

        _logger.LogInformation(
            "CanonicalDemographicsAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, string.Join(",", projection.Keys));

        return Ok(projection);
    }

    private async Task<DemographicsCanonical?> FetchAsync(int patientId, CancellationToken ct) =>
        CurrentScope.OriginScope switch
        {
            OriginScope.Hiso => MapHiso(await _repository.GetHisoAsync(
                patientId,
                new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId),
                ct)),
            OriginScope.Karo => MapKaro(await _repository.GetKaroAsync(patientId, RoutingContextFromScope(), ct)),
            OriginScope.Erms => MapErms(await _repository.GetErmsAsync(patientId, RoutingContextFromScope(), ct)),
            OriginScope.Col => MapCol(await _repository.GetColAsync(patientId, CurrentScope.PracticeId, ct)),
            _ => throw new InvalidOperationException($"Unhandled origin scope: {CurrentScope.OriginScope}")
        };

    /// <summary>
    /// Builds a RoutingContext from the token's claims: PracticeCode/Environment when the token was
    /// minted from a real EncounterId (via IKaroRoutingResolver/IErmsRoutingResolver), otherwise the
    /// RoutingContext.Unscoped sentinel - same fallback the tenant registry's flat-PracticeId overload uses.
    /// </summary>
    private RoutingContext RoutingContextFromScope() => new(
        CurrentScope.PracticeId,
        CurrentScope.PracticeCode ?? RoutingContext.Unscoped,
        CurrentScope.Environment ?? RoutingContext.Unscoped,
        CurrentScope.OriginScope);

    private static DemographicsCanonical? MapHiso(DemographicsHiso? d) => d is null ? null : new DemographicsCanonical(
        d.PatientId, d.PracticeId, d.FirstName, d.LastName, d.DateOfBirth, null, null, null, null, OriginScope.Hiso);

    private static DemographicsCanonical? MapKaro(DemographicsKaro? d) => d is null ? null : new DemographicsCanonical(
        d.PatientId, d.PracticeId, d.FirstName, d.LastName, d.DateOfBirth, d.DateOfEnrolment, d.EndEnrolmentDate, null, null, OriginScope.Karo);

    private static DemographicsCanonical? MapErms(DemographicsErms? d) => d is null ? null : new DemographicsCanonical(
        d.PatientId, null, d.FirstName, d.LastName, d.Dob, null, null, d.EncounterId, d.Nhi, OriginScope.Erms);

    private static DemographicsCanonical? MapCol(DemographicsCol? d) => d is null ? null : new DemographicsCanonical(
        d.PatientId, d.PracticeId, d.FirstName, d.LastName, null, null, null, null, null, OriginScope.Col);
}
