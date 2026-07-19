using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Adapters.Erms.Col;

/// <summary>
/// Translates COL/Pegasus's Authenticate wire shape into/out of the canonical contract. Always
/// assigns OriginScope.Col - distinct from OriginScope.Erms, since COL includes a financial
/// SaveInvoice write and the ADR follow-up decision log confirms it gets its own origin scope,
/// separate from ERMS's other eReferrals functions.
/// </summary>
public static class ColCredentialTranslator
{
    public const OriginScope Origin = OriginScope.Col;

    public static TokenRequest ToCanonical(ColCredential credential) =>
        new(
            Username: credential.Username,
            Password: credential.Password,
            PatientId: int.TryParse(credential.PatientId, out var patientId) ? patientId : null,
            EncounterId: null,
            PracticeId: null);

    public static TokenResponse ToLegacy(TokenResponse response) => response;
}
