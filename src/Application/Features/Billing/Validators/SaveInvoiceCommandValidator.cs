using FluentValidation;

namespace HekCoreApi.Application.Features.Billing.Commands;

public sealed class SaveInvoiceCommandValidator : AbstractValidator<SaveInvoiceCommand>
{
    public SaveInvoiceCommandValidator()
    {
        RuleFor(x => x.Input.ServiceCode).NotEmpty();
        RuleFor(x => x.Input.AmountInclGst).GreaterThanOrEqualTo(0);
    }
}
