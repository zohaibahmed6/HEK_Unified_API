using FluentValidation;
using HekCoreApi.Application.Features.Admin.Commands;

namespace HekCoreApi.Application.Features.Admin.Validators;

public sealed class RegisterPracticeCommandValidator : AbstractValidator<RegisterPracticeCommand>
{
    public RegisterPracticeCommandValidator()
    {
        RuleFor(x => x.Input.Name).NotEmpty();
        RuleFor(x => x.Input.DatabaseServerId).NotEmpty();
    }
}

public sealed class UpdatePracticeCommandValidator : AbstractValidator<UpdatePracticeCommand>
{
    public UpdatePracticeCommandValidator()
    {
        RuleFor(x => x.Input.Name).NotEmpty();
        RuleFor(x => x.Input.DatabaseServerId).NotEmpty();
    }
}
