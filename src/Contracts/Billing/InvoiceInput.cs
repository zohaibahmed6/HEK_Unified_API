namespace HekCoreApi.Contracts.Billing;

/// <summary>Field set from ERMS COL SaveInvoice (Contract Design doc Section 6.2) - KARO's Invoice model field list is undocumented (PROJECT_STATUS.md open item 17), flagged, not guessed.</summary>
public sealed record InvoiceInput(
    string ServiceCode,
    string? ServiceName,
    decimal AmountInclGst,
    string? Payee,
    string? ServiceProvider,
    DateOnly? ServiceDate,
    string? PegasusReference,
    string? ClaimShortCode);
