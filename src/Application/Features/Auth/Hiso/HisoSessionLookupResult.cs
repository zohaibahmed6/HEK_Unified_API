using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Features.Auth.Hiso;

public enum HisoSessionLookupStatus
{
    Success,
    NotFound,
    Expired
}

/// <summary>
/// Typed result instead of a thrown/swallowed exception - the caller (Adapters.Hiso) decides how to
/// log/respond. The actual security-event log emission happens in the handler itself via ILogger&lt;T&gt;
/// (a logging abstraction, not a concrete Infrastructure dependency - see ADR-012/PROJECT_STATUS.md
/// for the flagged interpretation of "Application has zero infrastructure dependencies").
/// </summary>
public sealed record HisoSessionLookupResult(HisoSessionLookupStatus Status, HisoSessionContext? Context);
