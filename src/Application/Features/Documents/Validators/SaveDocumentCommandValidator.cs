using FluentValidation;

namespace HekCoreApi.Application.Features.Documents.Commands;

public sealed class SaveDocumentCommandValidator : AbstractValidator<SaveDocumentCommand>
{
    public SaveDocumentCommandValidator()
    {
        RuleFor(x => x.Input.Direction).NotEmpty().Must(d => d is "in" or "out").WithMessage("direction must be 'in' or 'out'.");
        RuleFor(x => x.Input.ContentType).NotEmpty();
        RuleFor(x => x.Input.Content).NotEmpty();
    }
}
