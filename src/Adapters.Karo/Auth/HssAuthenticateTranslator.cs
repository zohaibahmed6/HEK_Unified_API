using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Adapters.Karo.Auth;

/// <summary>
/// Translates HSS Portal's exact existing Authenticate wire shape into/out of the canonical
/// contract. Always assigns OriginScope.Karo - origin is structurally determined by this being the
/// HSS compat entry point, never by the request's own "system" field (ADR-003's core correction:
/// an earlier draft proposed trusting a caller-supplied "System" field for origin scope and was
/// rejected on security review - see ADR-003 Context). The "system"/"pho" fields are accepted and
/// passed through only because the legacy wire shape includes them, never used for authorization.
/// </summary>
public static class HssAuthenticateTranslator
{
    public const OriginScope Origin = OriginScope.Karo;

    public static TokenRequest ToCanonical(HssAuthenticateRequest request) =>
        new(
            Username: request.Username,
            Password: request.Password,
            // KARO-BR-01: EncounterId is a composite key (<encryptedEncounterId>__<practiceId>[_<subPracticeId>]),
            // not a plain integer - decoding it requires the Rijndael decryption + delimiter-split logic that
            // belongs to Block 2's domain endpoints, not this Block 1 auth-only translator. Left unset here.
            PatientId: int.TryParse(request.PatientId, out var patientId) ? patientId : null,
            EncounterId: null,
            PracticeId: null);

    public static HssAuthenticateResponse ToLegacy(TokenResponse response) =>
        HssAuthenticateResponse.Success(response.Token, response.Expiry, response.PracticeId);
}
