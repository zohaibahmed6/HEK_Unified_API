namespace Api.IntegrationTests.LiveApi;

/// <summary>
/// Real test values confirmed working against the live dev stack (dbserver-local / PMS_NZ_V2,
/// DMS_PMS) during the 2026-07-28 manual verification session - not invented placeholders.
///
/// The encounter ID's 4-segment shape (id__practiceId__practiceCode__environment) matters:
/// KaroRoutingResolver/ErmsRoutingResolver split on "__" and only pick up a 4th (Environment)
/// segment when one exists. The TenantRegistry row actually seeded for practice 901 has
/// PracticeCode="-", Environment="local" - a 3-segment encounter ID (the one that used to sit as
/// the frontend dashboard's default) resolves to PracticeCode="FZZ999-B"/Environment="-", which
/// matches no row and fails every KARO/ERMS write with "practice ... is not registered."
/// </summary>
public static class KnownTestData
{
    public const string PatientId = "2459731";
    public const string EncounterId = "2147488418__901__-__local";

    public const string KaroUsername = "hsslive";
    public const string KaroPassword = "H$$L1v3005";
    public const string KaroSystem = "hss";
    public const string KaroPho = "NBPH0";

    public const string ErmsUsername = "ermsdev";
    public const string ErmsPassword = "eRMsd3V";

    public const string ColUsername = "indiCOLProd";
    public const string ColPassword = "C@L321$Prod!";

    /// <summary>
    /// A HISO session key confirmed live during this session. HISO sessions are minted through the
    /// real legacy SOAP session-creation flow, which this test project doesn't drive - if this key
    /// has since expired, HisoCompatTests will fail with a clear "session not found/expired" message
    /// rather than a silent false pass; replace with a freshly minted key when that happens.
    /// </summary>
    public const string HisoSessionKey = "0f456781-fbe5-41d8-a27b-8cac561ccaec";
}
