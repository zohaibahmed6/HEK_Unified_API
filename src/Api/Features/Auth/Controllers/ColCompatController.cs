using HekCoreApi.Adapters.Erms.Col;
using HekCoreApi.Application.Features.Auth.Commands;
using HekCoreApi.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// COL/Pegasus's compat Authenticate endpoint - JSON Credential shape, per Contract Design doc
/// Section 4.1. FLAGGED: COL is confirmed undocumented (SRS Section 4.3) - see ColCredential's own
/// remarks for the inference this endpoint's request shape rests on.
/// </summary>
[ApiController]
[Route("erms/col")]
public sealed class ColCompatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ColCompatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("authenticate")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Authenticate([FromBody] ColCredential credential, CancellationToken ct)
    {
        var canonicalRequest = ColCredentialTranslator.ToCanonical(credential);
        var result = await _mediator.Send(new AuthenticateCommand(canonicalRequest, ColCredentialTranslator.Origin), ct);

        return result is { Succeeded: true, Token: not null }
            ? Ok(ColCredentialTranslator.ToLegacy(result.Token))
            : Unauthorized();
    }
}
