using HekCoreApi.Application.Features.Auth.Commands;
using HekCoreApi.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// Canonical POST /auth/token (OpenAPI spec). Re-enabled 2026-07-22 as a demo/testing entry point for
/// the new canonical layer (HEK_UNIFIED_API_SPEC.md) - wired to the same real
/// <see cref="AuthenticateCommand"/>/<see cref="Application.Common.Interfaces.IJwtTokenIssuer"/>
/// pipeline every legacy compat authenticate endpoint already reuses.
///
/// STILL-OPEN ITEM (PROJECT_STATUS.md item 26): a production-ready answer to "what origin scope
/// does a genuinely direct, non-legacy caller get" is unresolved - ADR-003 says origin scope must be
/// structural (determined by which credential/entry-point authenticated), never self-reported.
/// <see cref="TokenRequest.OriginScope"/> being caller-supplied here is a deliberate, flagged
/// shortcut to make the canonical layer testable now, not a resolution of that item.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IssueToken([FromBody] TokenRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AuthenticateCommand(request, request.OriginScope), ct);
        return result.Succeeded ? Ok(result.Token) : Unauthorized();
    }
}
