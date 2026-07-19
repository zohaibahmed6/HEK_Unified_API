namespace HekCoreApi.Contracts.Security;

/// <summary>
/// Custom JWT claim-type constants carrying ResourceScope on issued tokens (ADR-003).
/// Named constants per coding-standards - never magic strings at call sites.
/// </summary>
public static class HekClaimTypes
{
    public const string PatientId = "hek:patientId";
    public const string EncounterId = "hek:encounterId";
    public const string PracticeId = "hek:practiceId";
    public const string OriginScope = "hek:originScope";
    public const string Scope = "hek:scope";
}
