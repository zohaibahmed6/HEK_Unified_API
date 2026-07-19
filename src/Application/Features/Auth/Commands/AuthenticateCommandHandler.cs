using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Auth.Commands;

/// <summary>
/// The single real authentication code path (ADR-002). Validates the credential against Entra ID,
/// then mints HEK Core API's own resource-scoped token (ADR-003). Legacy compat controllers reuse
/// this exact handler via their translators - only the request/response shape differs at the edge.
/// </summary>
public sealed class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, AuthenticateCommandResult>
{
    private readonly IIdentityValidator _identityValidator;
    private readonly IJwtTokenIssuer _tokenIssuer;

    public AuthenticateCommandHandler(IIdentityValidator identityValidator, IJwtTokenIssuer tokenIssuer)
    {
        _identityValidator = identityValidator;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<AuthenticateCommandResult> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        var validation = await _identityValidator.ValidateAsync(request.Request.Username, request.Request.Password, cancellationToken);
        if (!validation.Succeeded)
        {
            return new AuthenticateCommandResult(false, null);
        }

        var scope = new ResourceScope(
            PatientId: request.Request.PatientId?.ToString() ?? string.Empty,
            EncounterId: request.Request.EncounterId?.ToString(),
            PracticeId: request.Request.PracticeId ?? string.Empty,
            OriginScope: request.OriginScope);

        var token = await _tokenIssuer.IssueAsync(scope, cancellationToken);
        return new AuthenticateCommandResult(true, token);
    }
}
