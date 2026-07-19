namespace HekCoreApi.Adapters.Erms.Col;

/// <summary>
/// COL/Pegasus's Authenticate request shape. FLAGGED INFERENCE: the SRS explicitly describes COL as
/// "a second, undocumented JSON-based claiming-adjacent integration" (SRS Section 4.3) - no source
/// document gives its exact field list, only that it is "JSON Credential" (Contract Design doc
/// Section 4.1). This mirrors ERMS's documented XML Credential fields (Username/Password/PatientId/
/// EncounterId) translated to JSON, as the closest documented analog - needs confirmation against
/// the live COL/Pegasus consumer before being trusted as exact (PROJECT_STATUS.md).
/// </summary>
public sealed record ColCredential(string Username, string Password, string? PatientId, string? EncounterId);
