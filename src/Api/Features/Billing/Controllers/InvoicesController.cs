using HekCoreApi.Api.Controllers;
using HekCoreApi.Application.Features.Billing.Commands;
using HekCoreApi.Contracts.Billing;
using HekCoreApi.Contracts.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Billing.Controllers;

/// <summary>
/// Standard resource-scoped auth only (same as every other Block 2 write endpoint) - matches the real
/// legacy `COLController.SaveInvoice`, which required only a valid session token, no elevated/distinct
/// permission tier (PROJECT_STATUS.md open item 29, resolved 2026-07-20). An earlier build had this
/// endpoint behind an additional `billing:write` scope per an SRS recommendation (Section 12.2, citing
/// ERMS SEC-04) - but SEC-04 itself is about wildcard CORS, not a missing scope, and no legacy system
/// ever had a billing-specific permission concept. Zohaib decided: match the legacy system's actual
/// behavior instead. The `BillingWrite` policy stays defined (`AuthorizationPolicyNames.BillingWrite`,
/// `Program.cs`) in case a real scope-granting design is wanted later - just no longer applied here.
/// </summary>
[Route("patients/{patientId:int}/invoices")]
// DISABLED (2026-07-22, per Zohaib): only the legacy compat APIs (HISO /hiso, KARO /karo,
// ERMS /erms, COL /erms/col) are exposed. [NonController] removes this controller from routing
// and Swagger without deleting code - remove the attribute to re-enable.
[NonController]
public sealed class InvoicesController : ResourceScopedControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Save(int patientId, [FromBody] InvoiceInput input, CancellationToken ct)
    {
        EnsurePatientScope(patientId);

        var idempotencyKey = Request.Headers.TryGetValue(IdempotencyHeaderNames.IdempotencyKey, out var value) ? value.ToString() : null;
        var outcome = await _mediator.Send(new SaveInvoiceCommand(patientId, CurrentScope.EncounterId, CurrentScope.PracticeId, input, idempotencyKey), ct);

        return outcome.WasDuplicate ? Ok(outcome.Resource) : StatusCode(StatusCodes.Status201Created, outcome.Resource);
    }
}

