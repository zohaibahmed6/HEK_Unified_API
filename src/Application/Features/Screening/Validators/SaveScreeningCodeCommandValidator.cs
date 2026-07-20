using FluentValidation;

namespace HekCoreApi.Application.Features.Screening.Commands;

public sealed class SaveScreeningCodeCommandValidator : AbstractValidator<SaveScreeningCodeCommand>
{
    public SaveScreeningCodeCommandValidator()
    {
        RuleFor(x => x.Input.Code).NotEmpty();
    }
}
