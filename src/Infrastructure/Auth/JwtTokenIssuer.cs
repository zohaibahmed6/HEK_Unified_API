using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HekCoreApi.Infrastructure.Auth;

/// <summary>
/// Mints HEK Core API's own resource-scoped JWT (ADR-003), signed with a key resolved via
/// ISecretProvider - never a literal. Carries patientId/encounterId/practiceId/originScope claims,
/// distinct from any Entra ID token (see IJwtTokenIssuer remarks).
/// </summary>
public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private const string Issuer = "hek-core-api";
    private const string Audience = "hek-core-api-clients";

    private readonly ISecretProvider _secretProvider;
    private readonly AuthOptions _options;

    public JwtTokenIssuer(ISecretProvider secretProvider, IOptions<AuthOptions> options)
    {
        _secretProvider = secretProvider;
        _options = options.Value;
    }

    public async Task<TokenResponse> IssueAsync(ResourceScope scope, CancellationToken ct = default)
    {
        var signingKey = await ResolveSigningKeyAsync(ct);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiry = DateTimeOffset.UtcNow.AddHours(_options.TokenExpiryHours);

        var claims = new List<Claim>
        {
            new(HekClaimTypes.PatientId, scope.PatientId),
            new(HekClaimTypes.PracticeId, scope.PracticeId),
            new(HekClaimTypes.OriginScope, scope.OriginScope.ToString())
        };

        if (!string.IsNullOrEmpty(scope.EncounterId))
        {
            claims.Add(new Claim(HekClaimTypes.EncounterId, scope.EncounterId));
        }

        if (!string.IsNullOrEmpty(scope.PracticeCode))
        {
            claims.Add(new Claim(HekClaimTypes.PracticeCode, scope.PracticeCode));
        }

        if (!string.IsNullOrEmpty(scope.Environment))
        {
            claims.Add(new Claim(HekClaimTypes.Environment, scope.Environment));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiry.UtcDateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResponse(jwt, expiry, scope.PracticeId, scope.OriginScope);
    }

    private async Task<SymmetricSecurityKey> ResolveSigningKeyAsync(CancellationToken ct)
    {
        var secret = await _secretProvider.GetRequiredSecretAsync(_options.SigningKeySecretName, ct);
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }
}
