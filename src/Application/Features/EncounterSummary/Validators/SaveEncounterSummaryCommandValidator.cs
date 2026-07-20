using FluentValidation;

namespace HekCoreApi.Application.Features.EncounterSummary.Commands;

public sealed class SaveEncounterSummaryCommandValidator : AbstractValidator<SaveEncounterSummaryCommand>
{
    public SaveEncounterSummaryCommandValidator()
    {
        RuleFor(x => x.Input.Identifier).NotEmpty();
        RuleFor(x => x.Input.Fields).NotNull();
    }
}
