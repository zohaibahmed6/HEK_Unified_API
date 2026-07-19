using HekCoreApi.Adapters.Karo.Auth;
using HekCoreApi.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// HSS Portal's compat Authenticate endpoint - request/response body shape preserved exactly
/// (KARO_HSS_doc.md), including the legacy "always 200, status field in body" behavior, since that
/// is part of the wire contract HSS Portal already parses (ADR-002's "zero consumer-side change").
/// FLAGGED: the legacy system's original path is bare "/authenticate", hosted at HSS Portal's own
/// dedicated URL. This unified deployment (single Api host, Block 0/1) namespaces it as
/// "/karo/authenticate" to avoid a collision with ERMS's own "/authenticate" - a path-level
/// deviation from strict wire compatibility, needing confirmation on whether ADR-011's
/// designated-server/host-based routing is the intended real mechanism instead of a path prefix.
/// </summary>
[ApiController]
[Route("karo")]
public sealed class KaroCompatController : ControllerBase
{
    private readonly IMediator _mediator;

    public KaroCompatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("authenticate")]
    [HttpGet("authenticate")]
    [ProducesResponseType(typeof(HssAuthenticateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Authenticate([FromQuery] HssAuthenticateRequest? query, [FromBody] HssAuthenticateRequest? body, CancellationToken ct)
    {
        var legacyRequest = body ?? query;
        if (legacyRequest is null)
        {
            return Ok(HssAuthenticateResponse.Fail());
        }

        var canonicalRequest = HssAuthenticateTranslator.ToCanonical(legacyRequest);
        var result = await _mediator.Send(new AuthenticateCommand(canonicalRequest, HssAuthenticateTranslator.Origin), ct);

        return Ok(result is { Succeeded: true, Token: not null }
            ? HssAuthenticateTranslator.ToLegacy(result.Token)
            : HssAuthenticateResponse.Fail());
    }
}
