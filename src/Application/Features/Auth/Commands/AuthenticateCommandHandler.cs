using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Contracts.Security;
using MediatR;
using Microsoft.Extensions.Options;

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
    private readonly ISecretProvider _secretProvider;
    private readonly LegacyPracticeResolutionOptions _practiceResolutionOptions;

    public AuthenticateCommandHandler(
        IIdentityValidator identityValidator,
        IJwtTokenIssuer tokenIssuer,
        ISecretProvider secretProvider,
        IOptions<LegacyPracticeResolutionOptions> practiceResolutionOptions)
    {
        _identityValidator = identityValidator;
        _tokenIssuer = tokenIssuer;
        _secretProvider = secretProvider;
        _practiceResolutionOptions = practiceResolutionOptions.Value;
    }

    public async Task<AuthenticateCommandResult> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        var validation = await _identityValidator.ValidateAsync(request.Request.Username, request.Request.Password, cancellationToken);
        if (!validation.Succeeded)
        {
            return new AuthenticateCommandResult(false, null);
        }

        var practiceId = request.Request.PracticeId;
        if (string.IsNullOrEmpty(practiceId) && _practiceResolutionOptions.Enabled)
        {
            // Open item 32: while enabled and configured, replicates the legacy KARO/ERMS
            // Authenticate response's server-resolved practiceId via a username-keyed lookup.
            // Off by default - see LegacyPracticeResolutionOptions.
            practiceId = await _secretProvider.GetSecretAsync($"Auth:LegacyPracticeMappings:{request.Request.Username}", cancellationToken);
        }

        var scope = new ResourceScope(
            PatientId: request.Request.PatientId?.ToString() ?? string.Empty,
            EncounterId: request.Request.EncounterId?.ToString(),
            PracticeId: practiceId ?? string.Empty,
            OriginScope: request.OriginScope);

        var token = await _tokenIssuer.IssueAsync(scope, cancellationToken);
        return new AuthenticateCommandResult(true, token);
    }
}
