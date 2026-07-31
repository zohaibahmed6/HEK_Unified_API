namespace HekCoreApi.Api.Configuration;

/// <summary>
/// ADR-008-style toggle (ROI: real, working capability, off by default). When enabled, the
/// technical-*.log file (JSON, never readable-*.log) additionally carries the full request/response
/// payload for every legacy-compat call - the detail needed to diagnose a real production issue
/// ("why did this specific call return the wrong thing"), which the default logging deliberately
/// omits since it can contain PHI (NHI, name, DOB, clinical content). Meant to be switched on only
/// while actively investigating a reported issue, then switched back off - see
/// hek_analysis/LOGGING_OVERHAUL_PLAN.md for the full rationale and the access-control/encryption-at-rest
/// expectations that come with turning this on.
/// </summary>
public sealed class VerboseDiagnosticLoggingOptions
{
    public const string SectionName = "VerboseDiagnosticLogging";

    public bool Enabled { get; set; }
}
