namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// v1.1 spec follow-through, Step 2: real KARO and ERMS consumers both call the identical real path
/// shape `/api/{OperationName}` (confirmed from `Indici ERMS Web API Specification-v1.1.2.md` and
/// `indici_API_HSS_2.1.3_updated.md`), historically on separate dedicated hosts - colliding if both
/// are exposed at `/api/...` on this one hub. This config lets the real per-environment hostname
/// (10+ environments, editable here without a code change) decide which system a `/api/...` or
/// `/COL/...` call belongs to, then <c>LegacyHostRoutingMiddleware</c> (Api project) rewrites the
/// path onto the already-verified internal route (`/karo`, `/erms`, `/erms/col`) before MVC routing
/// runs - zero change to any already-tested controller, purely an edge rewrite. Any request whose
/// Host doesn't match a configured rule falls through unchanged, so the existing `/karo`/`/erms`
/// paths keep working exactly as before for local dev/testing.
/// </summary>
public sealed class LegacyHostRoutingOptions
{
    public const string SectionName = "LegacyHostRouting";

    public List<LegacyHostRoutingRule> Rules { get; set; } = [];
}

/// <summary>One host-pattern-to-system mapping. <see cref="HostContains"/> is matched case-insensitively
/// against the request's Host header (no wildcards needed - substring match, e.g. "hss" or "compliancehiso").</summary>
public sealed class LegacyHostRoutingRule
{
    public string HostContains { get; set; } = string.Empty;

    /// <summary>One of "Karo", "Erms", "Col" - matches the real external path prefix each system uses.</summary>
    public string System { get; set; } = string.Empty;
}
