using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Api.Telemetry;

/// <summary>
/// FR-12 (call-flow traceability): writes the resolved routing info onto the HTTP response as
/// additive `X-Hek-Routing-*` headers, plus a plain-English summary header for non-technical readers.
/// Never touches the response body - every legacy-compat endpoint's body must stay byte-identical to
/// legacy (FR-10), so headers are the only place this can safely ride.
/// </summary>
public static class RoutingHeaderWriter
{
    public static void WriteRoutingHeaders(this HttpResponse response, string system, CallRoutingInfo? routing)
    {
        if (routing is null)
        {
            return;
        }

        response.Headers["X-Hek-Routing-System"] = routing.SourceSystem;
        response.Headers["X-Hek-Routing-Environment"] = routing.Environment;
        response.Headers["X-Hek-Routing-DbServer"] = routing.DbServerHost;
        response.Headers["X-Hek-Routing-DbName"] = routing.DbName;
        response.Headers["X-Hek-Routing-PracticeId"] = routing.PracticeId;
        response.Headers["X-Hek-Routing-Summary"] = RoutingSummaryFormatter.BuildSummary(system, routing);
    }
}
