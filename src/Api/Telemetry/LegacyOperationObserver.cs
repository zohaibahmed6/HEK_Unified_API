using System.Diagnostics;

namespace HekCoreApi.Api.Telemetry;

/// <summary>
/// Centralizes "log + mark the current span as failed + increment the legacy error counter" for the
/// legacy compat controllers (Hiso/Erms/Col/Karo), whose wire contracts must stay byte-for-byte
/// identical to the systems they reproduce. HTTP status codes on these endpoints often can't signal
/// failure (Erms/Col/Karo always return 200), so this is the only place that failure becomes visible
/// server-side: a structured log line, an `Activity.Current` marked Error, and a `HekTelemetry` metric.
/// Never changes control flow beyond what each pattern already did (rethrow vs. swallow-to-message).
/// </summary>
public sealed class LegacyOperationObserver
{
    private readonly HekTelemetry _telemetry;

    public LegacyOperationObserver(HekTelemetry telemetry)
    {
        _telemetry = telemetry;
    }

    /// <summary>
    /// Propagate pattern (Hiso/Karo): runs <paramref name="action"/>, logs Information on success or
    /// Warning when <paramref name="isExpectedFailure"/> flags the result as an expected business
    /// failure (e.g. invalid session key). On an unhandled exception, logs Error, marks the span, records
    /// the metric, then rethrows so <c>GlobalExceptionHandler</c> still produces the response.
    /// </summary>
    public async Task<TResult> ObserveAsync<TResult>(
        ILogger logger,
        string system,
        string endpoint,
        IReadOnlyDictionary<string, object?> context,
        Func<Task<TResult>> action,
        Func<TResult, bool>? isExpectedFailure = null,
        string? expectedFailureReason = null)
    {
        TagActivity(system, endpoint, context);
        try
        {
            var result = await action();

            if (isExpectedFailure is not null && isExpectedFailure(result))
            {
                RecordExpectedFailure(logger, system, endpoint, expectedFailureReason ?? "ExpectedFailure", context);
            }
            else
            {
                LogSuccess(logger, system, endpoint, context);
            }

            return result;
        }
        catch (Exception ex)
        {
            RecordUnexpectedFailure(logger, system, endpoint, ex, context);
            throw;
        }
    }

    /// <summary>
    /// Swallow pattern (Erms/Col): runs <paramref name="action"/>. If it throws - the legacy
    /// <c>catch (Exception ex) { error = ex.Message; }</c> case - logs Error, marks the span, records the
    /// metric, and returns <c>(null, ex.Message)</c> instead of rethrowing, so callers can plug this in
    /// without changing their existing <c>SetToXml(result, error)</c>/<c>Json(...)</c> tail code.
    /// </summary>
    public async Task<(string? Result, string? Error)> ObserveSwallowedAsync(
        ILogger logger,
        string system,
        string endpoint,
        IReadOnlyDictionary<string, object?> context,
        Func<Task<string>> action)
    {
        TagActivity(system, endpoint, context);
        try
        {
            var result = await action();
            LogSuccess(logger, system, endpoint, context);
            return (result, null);
        }
        catch (Exception ex)
        {
            RecordUnexpectedFailure(logger, system, endpoint, ex, context);
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// For the "a Result object came back with Succeeded == false, no exception was thrown" case
    /// (invalid session key, invalid token, business-level failure). Logs Warning, marks the current
    /// span as Error (traces should surface these even though the legacy wire contract keeps 200/401
    /// unchanged), and records the error-rate metric.
    /// </summary>
    public void RecordExpectedFailure(ILogger logger, string system, string endpoint, string reason, IReadOnlyDictionary<string, object?> context)
    {
        TagActivity(system, endpoint, context);
        logger.LogWarning(
            "{System} {Endpoint}: expected failure ({Reason}). Context: {@Context}",
            system, endpoint, reason, context);

        Activity.Current?.SetStatus(ActivityStatusCode.Error, reason);

        _telemetry.RecordLegacyEndpointError(system, endpoint, reason);
    }

    /// <summary>Logs Error, marks the current span as failed, and records the error-rate metric for an exception that a caller is handling inline (e.g. inside its own try/catch/finally) rather than through <see cref="ObserveAsync{TResult}"/>/<see cref="ObserveSwallowedAsync"/>.</summary>
    public void RecordUnexpectedFailure(ILogger logger, string system, string endpoint, Exception ex, IReadOnlyDictionary<string, object?> context)
    {
        TagActivity(system, endpoint, context);
        logger.LogError(
            ex,
            "{System} {Endpoint}: unexpected exception. Context: {@Context}",
            system, endpoint, context);

        Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
        Activity.Current?.AddException(ex);

        _telemetry.RecordLegacyEndpointError(system, endpoint, ex.GetType().Name);
    }

    private static void LogSuccess(ILogger logger, string system, string endpoint, IReadOnlyDictionary<string, object?> context) =>
        logger.LogInformation(
            "{System} {Endpoint} succeeded. Context: {@Context}",
            system, endpoint, context);

    /// <summary>
    /// For controllers that don't route their success path through <see cref="ObserveAsync{TResult}"/>/
    /// <see cref="ObserveSwallowedAsync"/> at all (e.g. `KaroCompatController`, which only calls this
    /// class on the failure branch) - lets a success path still tag the trace span with real business
    /// context, without implying a warning/error the way `RecordExpectedFailure` would.
    /// </summary>
    public void Tag(string system, string endpoint, IReadOnlyDictionary<string, object?> context) => TagActivity(system, endpoint, context);

    /// <summary>
    /// Attaches business context (patientId, sessionKey, encounterId, etc.) directly onto the current
    /// trace span. Structured logs already carry this via `{@Context}`, but only traces/metrics are
    /// wired to the OTLP exporter (no `.WithLogging()` in Program.cs) - so without this, the Aspire
    /// dashboard's Traces view shows only the default ASP.NET Core attributes plus a correlation ID,
    /// none of the real request context. Tags are prefixed `legacy.` to avoid colliding with
    /// auto-instrumentation's own `http.*`/`otel.*` attributes. Safe to call more than once per span
    /// (e.g. both `ObserveAsync` and a later `RecordExpectedFailure` on the same request).
    /// </summary>
    private static void TagActivity(string system, string endpoint, IReadOnlyDictionary<string, object?> context)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("legacy.system", system);
        activity.SetTag("legacy.endpoint", endpoint);
        foreach (var (key, value) in context)
        {
            activity.SetTag($"legacy.{key}", value?.ToString());
        }
    }
}
