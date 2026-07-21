namespace HekCoreApi.Contracts.Billing;

/// <summary>
/// Field set confirmed directly from ERMS COL's real source (legacy-reference/controller/COLController.cs's
/// `SaveInvoice` request model, backed by legacy-reference/DAL/HSS/HSSDA.cs's `InsertUpdateService`) -
/// PROJECT_STATUS.md open item 17, closed. `Description`, `AccountHolderId`, `ServiceProviderType` are
/// carried forward here because the old system accepted them, even though the earlier inferred version
/// of this contract omitted them. KARO's own SaveInvoice implementation was never supplied, so this
/// contract is confirmed for the ERMS/COL calling path specifically.
/// </summary>
public sealed record InvoiceInput(
    string ServiceCode,
    string? ServiceName,
    decimal AmountInclGst,
    string? Description,
    string? AccountHolderId,
    string? Payee,
    string? ServiceProvider,
    string? ServiceProviderType,
    DateOnly? ServiceDate,
    string? PegasusReference,
    string? ClaimShortCode);
