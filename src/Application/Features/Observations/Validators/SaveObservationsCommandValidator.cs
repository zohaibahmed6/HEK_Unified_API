using FluentValidation;

namespace HekCoreApi.Application.Features.Observations.Commands;

/// <summary>KARO-BR-14: at least one of the nine measurement fields must be non-empty.</summary>
public sealed class SaveObservationsCommandValidator : AbstractValidator<SaveObservationsCommand>
{
    public SaveObservationsCommandValidator()
    {
        RuleFor(x => x.Input).Must(input => input.HasAnyValue()).WithMessage("At least one measurement field is required.");
    }
}
