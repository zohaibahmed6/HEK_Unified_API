using HekCoreApi.Contracts.Billing;

namespace HekCoreApi.Application.Common.Interfaces;

public interface IInvoicesRepository
{
    /// <summary>Natural key: same serviceCode + serviceDate for the patient (replacing KARO's/ERMS COL's -3 magic code, Contract Design doc Section 8 Decision 3).</summary>
    Task<Invoice?> FindByNaturalKeyAsync(int patientId, string practiceId, string serviceCode, DateOnly? serviceDate, CancellationToken ct = default);

    /// <summary>
    /// Returns (Invoice, WasDuplicate). WasDuplicate is set when the underlying legacy stored
    /// procedure itself reports the invoice already exists (its own `-3` return code, confirmed
    /// via legacy-reference/DAL/HSS/HSSDA.cs) - a defense-in-depth signal on top of, not a
    /// replacement for, the platform's own Idempotency-Key/natural-key contract (Contract Design
    /// doc Section 12 Decision 3).
    /// </summary>
    Task<(Invoice Invoice, bool WasDuplicate)> SaveAsync(int patientId, string? encounterId, string practiceId, InvoiceInput input, CancellationToken ct = default);
}
