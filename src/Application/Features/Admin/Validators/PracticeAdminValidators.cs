using FluentValidation;
using HekCoreApi.Application.Features.Admin.Commands;

namespace HekCoreApi.Application.Features.Admin.Validators;

public sealed class RegisterPracticeCommandValidator : AbstractValidator<RegisterPracticeCommand>
{
    public RegisterPracticeCommandValidator()
    {
        RuleFor(x => x.Input.Name).NotEmpty();
        RuleFor(x => x.Input.DbServerHost).NotEmpty();
        RuleFor(x => x.Input.DbName).NotEmpty();
        RuleFor(x => x.Input.SourceSystem).IsInEnum();
    }
}

public sealed class UpdatePracticeCommandValidator : AbstractValidator<UpdatePracticeCommand>
{
    public UpdatePracticeCommandValidator()
    {
        RuleFor(x => x.Input.Name).NotEmpty();
        RuleFor(x => x.Input.DbServerHost).NotEmpty();
        RuleFor(x => x.Input.DbName).NotEmpty();
        RuleFor(x => x.Input.SourceSystem).IsInEnum();
    }
}
