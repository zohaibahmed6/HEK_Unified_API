using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Providers;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>
/// Eighth canonical resource (2026-07-23), single-record shape (same style as
/// <see cref="CanonicalDemographicsController"/>) rather than a list, since KARO/ERMS/HISO all treat
/// this as "the one logged-in provider record", not a collection.
/// </summary>
[Route("v1/patients/{patientId:int}/currentprovider")]
public sealed class CanonicalCurrentProviderController : ResourceScopedControllerBase
{
    private static readonly IReadOnlyDictionary<OriginScope, IReadOnlyCollection<string>> AllowedFieldsByOrigin =
        new Dictionary<OriginScope, IReadOnlyCollection<string>>
        {
            [OriginScope.Hiso] = new[] { nameof(CurrentProviderCanonical.GivenName), nameof(CurrentProviderCanonical.FamilyName), nameof(CurrentProviderCanonical.Email) },
            [OriginScope.Karo] = new[] { nameof(CurrentProviderCanonical.GivenName), nameof(CurrentProviderCanonical.FamilyName), nameof(CurrentProviderCanonical.Email), nameof(CurrentProviderCanonical.Phone) },
            [OriginScope.Erms] = new[] { nameof(CurrentProviderCanonical.GivenName), nameof(CurrentProviderCanonical.FamilyName), nameof(CurrentProviderCanonical.Email), nameof(CurrentProviderCanonical.Phone) }
        };

    private readonly ICanonicalCurrentProviderRepository _repository;
    private readonly ILogger<CanonicalCurrentProviderController> _logger;

    public CanonicalCurrentProviderController(ICanonicalCurrentProviderRepository repository, ILogger<CanonicalCurrentProviderController> logger)
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
                detail: $"Current provider is not yet available for origin '{CurrentScope.OriginScope}'.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var provider = await FetchAsync(patientId, ct);
        if (provider is null)
        {
            return NotFound();
        }

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projection = FieldSelector.Project(provider, requestedFields, allowedFields);

        _logger.LogInformation(
            "CanonicalCurrentProviderAccess consumer={OriginScope} practiceId={PracticeId} patientId={PatientId} endpoint={Endpoint} fieldsReturned={FieldsReturned}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, patientId, Request.Path, string.Join(",", projection.Keys));

        return Ok(projection);
    }

    private async Task<CurrentProviderCanonical?> FetchAsync(int patientId, CancellationToken ct) =>
        CurrentScope.OriginScope switch
        {
            OriginScope.Hiso => await _repository.GetHisoAsync(
                new HealthLinkSession(CurrentScope.PatientId, string.Empty, CurrentScope.EncounterId ?? string.Empty, CurrentScope.PracticeId),
                ct),
            OriginScope.Karo => await _repository.GetKaroAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            OriginScope.Erms => await _repository.GetErmsAsync(RoutingContextFromScope(), CurrentScope.PatientId, ct),
            _ => null
        };

    private RoutingContext RoutingContextFromScope() => new(
        CurrentScope.PracticeId,
        CurrentScope.PracticeCode ?? RoutingContext.Unscoped,
        CurrentScope.Environment ?? RoutingContext.Unscoped,
        CurrentScope.OriginScope);
}
