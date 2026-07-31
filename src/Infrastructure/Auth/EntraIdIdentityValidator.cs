using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Infrastructure.Auth;

/// <summary>
/// Validates a caller-supplied username/password pair for the local `/auth/token` and legacy
/// compat authenticate endpoints (ADR-002/ADR-003).
///
/// CORRECTED 2026-07-30 (auth research log item 12/13): the previous implementation called
/// <c>AcquireTokenForClient</c> - Entra ID's app-only Client Credentials flow, which proves *our
/// own gateway app's* identity to Entra, using our own registered ClientId/ClientSecret. It never
/// inspected the username/password the caller actually supplied, so once `Auth:Enabled=true` it
/// would treat any non-empty credential as valid - equivalent to a guard checking his own ID
/// instead of the visitor's.
///
/// The correct shape for Entra ID in an app-to-app world (our callers are always systems, never a
/// human browser login - confirmed) is: each calling application gets its own Entra App
/// Registration, acquires its own token, and presents that token to us directly as
/// `Authorization: Bearer` - validated by the separate `EntraBearer` JWT bearer scheme registered
/// in Program.cs, not by this class. Resource-Owner-Password-Credentials (forwarding a raw
/// username/password to an IdP) is a deprecated OAuth pattern Microsoft itself discourages, so this
/// class does not attempt it. When the auth toggle is on and no real credential source is wired for
/// this specific endpoint, it fails closed (rejects) rather than fabricating a "verified" result.
///
/// ADR-008 config toggle: when Auth:Enabled is false (the default), this runs in the explicitly
/// accepted "legacy-equivalent" mode - any non-empty credential is treated as valid, exactly
/// mirroring the fact that none of the three legacy systems performs real authentication today.
/// This is a knowingly accepted risk (ADR-008 Consequences), not an oversight.
/// </summary>
public sealed class EntraIdIdentityValidator : IIdentityValidator
{
    private readonly AuthOptions _options;
    private readonly ILogger<EntraIdIdentityValidator> _logger;

    public EntraIdIdentityValidator(IOptions<AuthOptions> options, ILogger<EntraIdIdentityValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IdentityValidationResult> ValidateAsync(string username, string password, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning(
                "Auth toggle (ADR-008) is disabled - running in legacy-equivalent mode. Credential for {Username} was not cryptographically validated.",
                username);
            return Task.FromResult(new IdentityValidationResult(Succeeded: !string.IsNullOrWhiteSpace(username), Subject: username));
        }

        // Fail closed: this path intentionally does not forward the caller's username/password to
        // Entra ID (ROPC is deprecated, and it isn't the right model for our app-to-app callers
        // anyway). Real callers should authenticate directly against Entra ID and present the
        // resulting token to the `EntraBearer` scheme instead of calling this username/password path.
        _logger.LogWarning(
            "Auth is enabled but username/password validation against Entra ID is not supported (deprecated ROPC pattern) - rejecting credential for {Username}. Use the EntraBearer inbound-token path instead.",
            username);
        return Task.FromResult(new IdentityValidationResult(Succeeded: false, Subject: null));
    }
}
