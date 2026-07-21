using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.PracticeContext.Queries;
using HekCoreApi.Contracts.Security;
using HekCoreApi.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.PracticeContext.Controllers;

[Route("practices/{practiceId}/context")]
// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class PracticeContextController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public PracticeContextController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string practiceId, CancellationToken ct)
    {
        EnsureOriginScope(OriginScope.Col);
        if (practiceId != CurrentScope.PracticeId)
        {
            throw new ForbiddenException("Token is not scoped to the requested practice.");
        }

        var result = await _mediator.Send(new GetPracticeContextQuery(practiceId), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

