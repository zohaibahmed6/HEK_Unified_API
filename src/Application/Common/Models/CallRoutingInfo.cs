namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// FR-12 (call-flow traceability): which system/practice/environment/database server actually
/// fulfilled a request. Built from whichever routing lookup a given legacy-compat pipeline already
/// performs (the tenant registry's <see cref="PracticeRoute"/> for Karo/Erms/Col,
/// <see cref="HisoSessionRoute"/> for Hiso) - never a new DB lookup of its own. Purely additive:
/// carried on result records so the Api layer can surface it as response headers/trace tags without
/// ever touching a legacy-compat response body (FR-10 zero-change guarantee).
/// </summary>
public sealed record CallRoutingInfo(string SourceSystem, string PracticeId, string Environment, string DbServerHost, string DbName)
{
    public static CallRoutingInfo FromPracticeRoute(PracticeRoute route, string environment) =>
        new(route.SourceSystem, route.PracticeId, environment, route.DbServerHost, route.DbName);

    public static CallRoutingInfo FromHisoSessionRoute(HisoSessionRoute route) =>
        new("Hiso", route.PracticeId, route.Environment, route.DbServerHost, route.DbName);
}
