using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// Everything the tenant registry needs to route a request to the right physical database,
/// independent of which source system produced it. Built by a source-specific resolver
/// (<c>IKaroRoutingResolver</c>/<c>IErmsRoutingResolver</c>/<c>IHisoRoutingResolver</c>) from that
/// system's own real identifier (KARO/ERMS: <c>EncounterId</c>; HISO: the resolved session context) -
/// everything downstream of routing works with this shape only, never a source-specific one.
/// </summary>
public sealed record RoutingContext(string PracticeId, string PracticeCode, string Environment, OriginScope SourceSystem)
{
    /// <summary>Sentinel used by callers that only ever had a flat PracticeId (HISO, and every non-KARO/ERMS Block 2 repository).</summary>
    public const string Unscoped = "-";

    public static RoutingContext FromPracticeId(string practiceId, OriginScope sourceSystem) =>
        new(practiceId, Unscoped, Unscoped, sourceSystem);
}
