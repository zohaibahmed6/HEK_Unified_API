using FluentValidation;

namespace HekCoreApi.Application.Features.Tasks.Commands;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Input.ConceptCode).NotEmpty();
        RuleFor(x => x.Input.Description).NotEmpty();
    }
}
