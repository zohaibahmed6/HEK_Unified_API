using HekCoreApi.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// Canonical POST /auth/token (OpenAPI spec). FLAGGED OPEN ITEM: the OpenAPI TokenResponse.originScope
/// enum only defines Hiso|Karo|Erms|Col - all four legacy entry points. No source document states
/// what origin scope a direct (non-legacy) caller of this canonical endpoint should get, and
/// fabricating a fifth value not in the spec would violate the project's "never invent a field that
/// isn't in the contract" rule. Every real caller today goes through a legacy compat endpoint
/// instead (see Adapters.Karo/Adapters.Erms compat controllers), each of which hardcodes its own
/// origin scope. This endpoint is reserved for a future native client and returns 501 until that
/// origin-scope question is resolved with the stakeholder - see PROJECT_STATUS.md.
/// </summary>
[ApiController]
[Route("auth")]
// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class AuthController : ControllerBase
{
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult IssueToken([FromBody] TokenRequest request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
