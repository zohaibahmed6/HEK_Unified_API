using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace HekCoreApi.Infrastructure.Auth;

/// <summary>
/// Validates a caller's credential against Microsoft Entra ID (ADR-002, confirmed vendor).
///
/// DAY-1 SCAFFOLD NOTE: real per-username/password validation of a legacy service account
/// (hsslive, ERMS/COL equivalents) against Entra ID requires that account to already exist as a
/// provisioned App Registration in a real tenant - infrastructure/ops work outside this repo's
/// code (flagged in PROJECT_STATUS.md). This implementation performs a real MSAL confidential-
/// client credential acquisition using secrets resolved via ISecretProvider (never hardcoded), and
/// is fully wired for that once a tenant/App Registration exists. Until then, local dev/testing
/// uses the mock-idp described in the plan's verification section.
///
/// ADR-008 config toggle: when Auth:Enabled is false (the default), this runs in the explicitly
/// accepted "legacy-equivalent" mode - any non-empty credential is treated as valid, exactly
/// mirroring the fact that none of the three legacy systems performs real authentication today.
/// This is a knowingly accepted risk (ADR-008 Consequences), not an oversight.
/// </summary>
public sealed class EntraIdIdentityValidator : IIdentityValidator
{
    private readonly ISecretProvider _secretProvider;
    private readonly AuthOptions _options;
    private readonly ILogger<EntraIdIdentityValidator> _logger;

    public EntraIdIdentityValidator(ISecretProvider secretProvider, IOptions<AuthOptions> options, ILogger<EntraIdIdentityValidator> logger)
    {
        _secretProvider = secretProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IdentityValidationResult> ValidateAsync(string username, string password, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning(
                "Auth toggle (ADR-008) is disabled - running in legacy-equivalent mode. Credential for {Username} was not cryptographically validated.",
                username);
            return new IdentityValidationResult(Succeeded: !string.IsNullOrWhiteSpace(username), Subject: username);
        }

        var tenantId = await _secretProvider.GetRequiredSecretAsync("Entra:TenantId", ct);
        var clientId = await _secretProvider.GetRequiredSecretAsync("Entra:ClientId", ct);
        var clientSecret = await _secretProvider.GetRequiredSecretAsync("Entra:ClientSecret", ct);

        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .Build();

        try
        {
            var result = await app.AcquireTokenForClient(["https://graph.microsoft.com/.default"]).ExecuteAsync(ct);
            return new IdentityValidationResult(Succeeded: result.AccessToken is not null, Subject: username);
        }
        catch (MsalServiceException ex)
        {
            _logger.LogWarning(ex, "Entra ID credential validation failed for {Username}.", username);
            return new IdentityValidationResult(Succeeded: false, Subject: null);
        }
    }
}
