namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// Config for the ported DMSDA.cs dormant module (`Infrastructure/Legacy/Dormant/Dmsda`). Replaces
/// the legacy Web.config appSettings ("DMSDocTypes", "SqlCommandTimeOutInSeconds") that the original
/// class read directly via ConfigurationManager.AppSettings - per coding-standards' IOptions
/// pattern, not a hardcoded/ConfigurationManager dependency.
/// </summary>
public sealed class LegacyDmsOptions
{
    public const string SectionName = "LegacyDms";

    /// <summary>Pipe-delimited "id,extension" pairs, matching the legacy "DMSDocTypes" appSetting format exactly.</summary>
    public string DocumentTypes { get; set; } = string.Empty;

    public int SqlCommandTimeoutSeconds { get; set; } = 30;
}
