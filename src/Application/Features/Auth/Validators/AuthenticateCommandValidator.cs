using FluentValidation;

namespace HekCoreApi.Application.Features.Auth.Commands;

/// <summary>Only validates what the OpenAPI TokenRequest schema actually requires (username, password) - nothing invented beyond the contract.</summary>
public sealed class AuthenticateCommandValidator : AbstractValidator<AuthenticateCommand>
{
    public AuthenticateCommandValidator()
    {
        RuleFor(x => x.Request.Username).NotEmpty();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}
