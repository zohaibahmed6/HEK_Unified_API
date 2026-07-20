using FluentValidation;

namespace HekCoreApi.Application.Features.ClinicalNotes.Commands;

public sealed class SaveClinicalNoteCommandValidator : AbstractValidator<SaveClinicalNoteCommand>
{
    public SaveClinicalNoteCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty();
    }
}
