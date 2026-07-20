using FluentValidation;

namespace HekCoreApi.Application.Features.Conditions.Commands;

public sealed class SaveConditionCommandValidator : AbstractValidator<SaveConditionCommand>
{
    public SaveConditionCommandValidator()
    {
        RuleFor(x => x.Input.DiagnosisCode).NotEmpty();
    }
}
