using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Screening;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>KARO-only canonical resource (2026-07-23) - confirmed no matching HISO concept exists and ERMS has no screening-codes operation; other origins get a clean 501.</summary>
[Route("v1/screeningcodes")]
public sealed class CanonicalScreeningCodesController : ResourceScopedControllerBase
{
    private static readonly string[] AllowedFields = { nameof(ScreeningCodeCanonical.ConceptId), nameof(ScreeningCodeCanonical.ScreeningShortName), nameof(ScreeningCodeCanonical.ScreeningName) };

    private readonly ICanonicalScreeningCodesRepository _repository;
    private readonly ILogger<CanonicalScreeningCodesController> _logger;

    public CanonicalScreeningCodesController(ICanonicalScreeningCodesRepository repository, ILogger<CanonicalScreeningCodesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? fields, CancellationToken ct)
    {
        if (CurrentScope.OriginScope != OriginScope.Karo)
        {
            return Problem(
                title: "Not Supported",
                detail: $"Screening codes are not available for origin '{CurrentScope.OriginScope}' - KARO-only.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var routing = new RoutingContext(CurrentScope.PracticeId, CurrentScope.PracticeCode ?? RoutingContext.Unscoped, CurrentScope.Environment ?? RoutingContext.Unscoped, CurrentScope.OriginScope);
        var codes = await _repository.GetKaroAsync(routing, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = codes.Select(c => FieldSelector.Project(c, requestedFields, AllowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalScreeningCodesAccess consumer={OriginScope} practiceId={PracticeId} endpoint={Endpoint} itemCount={ItemCount}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, Request.Path, projections.Count);

        return Ok(new { items = projections });
    }
}
