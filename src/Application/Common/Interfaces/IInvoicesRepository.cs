using HekCoreApi.Contracts.Billing;

namespace HekCoreApi.Application.Common.Interfaces;

public interface IInvoicesRepository
{
    /// <summary>Natural key: same serviceCode + serviceDate for the patient (replacing KARO's/ERMS COL's -3 magic code, Contract Design doc Section 8 Decision 3).</summary>
    Task<Invoice?> FindByNaturalKeyAsync(int patientId, string practiceId, string serviceCode, DateOnly? serviceDate, CancellationToken ct = default);

    Task<Invoice> SaveAsync(int patientId, string practiceId, InvoiceInput input, CancellationToken ct = default);
}
