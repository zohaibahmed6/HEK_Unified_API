using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Recalls;
using HekCoreApi.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Canonical.Controllers;

/// <summary>KARO-only canonical resource (2026-07-23) - confirmed no matching HISO concept exists and ERMS has no recall-categories operation; other origins get a clean 501.</summary>
[Route("v1/recallcategories")]
public sealed class CanonicalRecallCategoriesController : ResourceScopedControllerBase
{
    private static readonly string[] AllowedFields = { nameof(RecallCategoryCanonical.Id), nameof(RecallCategoryCanonical.Name), nameof(RecallCategoryCanonical.Code) };

    private readonly ICanonicalRecallCategoriesRepository _repository;
    private readonly ILogger<CanonicalRecallCategoriesController> _logger;

    public CanonicalRecallCategoriesController(ICanonicalRecallCategoriesRepository repository, ILogger<CanonicalRecallCategoriesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? group, [FromQuery] string? fields, CancellationToken ct)
    {
        if (CurrentScope.OriginScope != OriginScope.Karo)
        {
            return Problem(
                title: "Not Supported",
                detail: $"Recall categories are not available for origin '{CurrentScope.OriginScope}' - KARO-only.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        var routing = new RoutingContext(CurrentScope.PracticeId, CurrentScope.PracticeCode ?? RoutingContext.Unscoped, CurrentScope.Environment ?? RoutingContext.Unscoped, CurrentScope.OriginScope);
        var categories = await _repository.GetKaroAsync(routing, group, ct);

        var requestedFields = string.IsNullOrWhiteSpace(fields)
            ? null
            : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var projections = categories.Select(c => FieldSelector.Project(c, requestedFields, AllowedFields)).ToList();

        _logger.LogInformation(
            "CanonicalRecallCategoriesAccess consumer={OriginScope} practiceId={PracticeId} endpoint={Endpoint} itemCount={ItemCount}",
            CurrentScope.OriginScope, CurrentScope.PracticeId, Request.Path, projections.Count);

        return Ok(new { items = projections });
    }
}
