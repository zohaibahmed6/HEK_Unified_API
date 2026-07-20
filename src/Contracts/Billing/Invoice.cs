namespace HekCoreApi.Contracts.Billing;

public sealed record Invoice(
    string InvoiceId,
    string Status,
    string ServiceCode,
    string? ServiceName,
    decimal AmountInclGst,
    string? Payee,
    string? ServiceProvider,
    DateOnly? ServiceDate,
    string? PegasusReference,
    string? ClaimShortCode)
{
    public static Invoice FromInput(string invoiceId, string status, InvoiceInput input) =>
        new(invoiceId, status, input.ServiceCode, input.ServiceName, input.AmountInclGst, input.Payee, input.ServiceProvider, input.ServiceDate, input.PegasusReference, input.ClaimShortCode);
}
