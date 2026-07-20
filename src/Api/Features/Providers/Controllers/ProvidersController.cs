using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.Providers.Queries;
using HekCoreApi.Contracts;
using HekCoreApi.Contracts.Providers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Providers.Controllers;

[Route("providers")]
public sealed class ProvidersController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? practiceLocationId, CancellationToken ct)
    {
        var providers = await _mediator.Send(new GetProvidersQuery(CurrentScope.PracticeId, practiceLocationId), ct);
        return Ok(new ListResponse<Provider>(providers));
    }
}
