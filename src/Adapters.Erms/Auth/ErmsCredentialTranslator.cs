using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Adapters.Erms.Auth;

/// <summary>
/// Translates ERMS eReferrals' exact existing XML Credential wire shape into/out of the canonical
/// contract. Always assigns OriginScope.Erms - structurally, by being the ERMS compat entry point.
/// </summary>
public static class ErmsCredentialTranslator
{
    public const OriginScope Origin = OriginScope.Erms;

    public static TokenRequest ToCanonical(ErmsCredential credential) =>
        new(
            Username: credential.Username,
            Password: credential.Password,
            PatientId: int.TryParse(credential.PatientId, out var patientId) ? patientId : null,
            // ERMS-BR-01/02/03: EncounterId may be AES-obfuscated and/or Base64-layered - decoding
            // it is Block 2 domain-endpoint logic, not this Block 1 auth-only translator.
            EncounterId: null,
            PracticeId: null);

    public static ErmsAuthenticationResponse ToLegacy(TokenResponse response) =>
        new()
        {
            Token = response.Token,
            Expiry = response.Expiry.ToString("O"),
            PracticeId = response.PracticeId
        };
}
