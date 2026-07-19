namespace HekCoreApi.Api.Configuration;

/// <summary>
/// Explicit CORS allow-list, replacing legacy's wildcard/wide-open (KARO) and inconsistent
/// (ERMS: wildcard on COLController, commented-out on APIController) configuration. No real
/// consumer origins are given in any source document - AllowedOrigins ships with a localhost-only
/// dev placeholder; the production allow-list is a deployment-config item to confirm before go-live.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Default";

    public string[] AllowedOrigins { get; set; } = ["https://localhost:5173", "https://localhost:3000"];
}
