using HekCoreApi.Api.Controllers;
using HekCoreApi.Api.Security;
using HekCoreApi.Application.Features.Billing.Commands;
using HekCoreApi.Contracts.Billing;
using HekCoreApi.Contracts.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Billing.Controllers;

/// <summary>
/// Requires billing:write scope, distinct from clinical read scope (Contract Design doc Section 13,
/// SRS Section 12.2, ERMS SEC-04). FLAGGED GAP: no source document specifies how/when a caller is
/// granted the billing:write scope claim - Block 1's JwtTokenIssuer does not currently mint it for
/// any caller, so in practice this endpoint rejects every caller until scope-granting is designed.
/// Implemented faithfully (the check is real, not bypassed) rather than silently working around the gap.
/// </summary>
[Route("patients/{patientId:int}/invoices")]
public sealed class InvoicesController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.BillingWrite)]
    public async Task<IActionResult> Save(int patientId, [FromBody] InvoiceInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var idempotencyKey = Request.Headers.TryGetValue(IdempotencyHeaderNames.IdempotencyKey, out var value) ? value.ToString() : null;
        var outcome = await _mediator.Send(new SaveInvoiceCommand(patientId, CurrentScope.PracticeId, input, idempotencyKey), ct);

        return outcome.WasDuplicate ? Ok(outcome.Resource) : StatusCode(StatusCodes.Status201Created, outcome.Resource);
    }
}
