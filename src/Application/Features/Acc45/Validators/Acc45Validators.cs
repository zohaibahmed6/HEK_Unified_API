using FluentValidation;
using HekCoreApi.Application.Features.Acc45.Commands;

namespace HekCoreApi.Application.Features.Acc45.Validators;

public sealed class SaveAcc45FormCommandValidator : AbstractValidator<SaveAcc45FormCommand>
{
    public SaveAcc45FormCommandValidator()
    {
        RuleFor(x => x.Input.DataContainer).NotNull();
    }
}

public sealed class DispatchAcc45ActionCommandValidator : AbstractValidator<DispatchAcc45ActionCommand>
{
    private static readonly string[] AllowedActions = ["save", "addTask", "addInvoice", "launchForm"];

    public DispatchAcc45ActionCommandValidator()
    {
        RuleFor(x => x.Input.ActionId).NotEmpty().Must(a => AllowedActions.Contains(a)).WithMessage("actionId must be one of save, addTask, addInvoice, launchForm.");
    }
}
