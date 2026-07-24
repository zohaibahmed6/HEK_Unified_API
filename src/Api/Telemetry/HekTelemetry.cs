using System.Diagnostics.Metrics;

namespace HekCoreApi.Api.Telemetry;

/// <summary>
/// Custom metrics on top of OpenTelemetry's automatic ASP.NET Core/SqlClient instrumentation
/// (Program.cs). One shared Meter, registered once in DI as a singleton so every controller uses the
/// same instrument instances rather than creating duplicates per request.
/// </summary>
public sealed class HekTelemetry
{
    public const string MeterName = "HekCoreApi";

    private readonly Counter<long> _canonicalFieldsReturned;
    private readonly Counter<long> _canonicalFieldsBlocked;
    private readonly Counter<long> _legacyEndpointErrors;

    public HekTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _canonicalFieldsReturned = meter.CreateCounter<long>(
            "hek.canonical.fields_returned",
            description: "Fields actually returned by a canonical hub endpoint, tagged by origin scope.");
        _canonicalFieldsBlocked = meter.CreateCounter<long>(
            "hek.canonical.fields_blocked",
            description: "Fields a caller requested but were outside its origin scope and were silently dropped (FR-5).");
        _legacyEndpointErrors = meter.CreateCounter<long>(
            "hek.legacy.endpoint_errors",
            description: "Failures observed on a legacy compat endpoint, tagged by system/endpoint/error_type, independent of HTTP status code.");
    }

    /// <summary>Records one legacy compat endpoint failure (expected business failure or unexpected exception), regardless of the HTTP status the legacy wire contract forces the response to use.</summary>
    public void RecordLegacyEndpointError(string system, string endpoint, string errorType)
    {
        _legacyEndpointErrors.Add(1, new KeyValuePair<string, object?>[]
        {
            new("system", system),
            new("endpoint", endpoint),
            new("error_type", errorType)
        });
    }

    /// <summary>Records one canonical hub call's field-scoping outcome (FR-5/FR-6 demo metric).</summary>
    public void RecordFieldScoping(string originScope, string endpoint, int returnedCount, int blockedCount)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("origin_scope", originScope),
            new("endpoint", endpoint)
        };
        _canonicalFieldsReturned.Add(returnedCount, tags);
        _canonicalFieldsBlocked.Add(blockedCount, tags);
    }
}
