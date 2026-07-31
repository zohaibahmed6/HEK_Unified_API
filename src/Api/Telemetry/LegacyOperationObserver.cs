using System.Diagnostics;
using HekCoreApi.Api.Configuration;
using HekCoreApi.Application.Common.Models;
using Microsoft.Extensions.Options;

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
    private readonly IOptionsMonitor<VerboseDiagnosticLoggingOptions> _verboseOptions;

    public LegacyOperationObserver(HekTelemetry telemetry, IOptionsMonitor<VerboseDiagnosticLoggingOptions> verboseOptions)
    {
        _telemetry = telemetry;
        _verboseOptions = verboseOptions;
    }

    /// <summary>
    /// Verbose diagnostic logging (off by default, see <see cref="VerboseDiagnosticLoggingOptions"/> and
    /// hek_analysis/LOGGING_OVERHAUL_PLAN.md): when enabled, logs the full request/response payload for
    /// this call to the technical-*.log file only - the readable-file sub-logger's "[CallLog]" filter
    /// and the errors sub-logger's Level&gt;=Warning filter both naturally exclude this Information-level,
    /// non-"[CallLog]"-tagged line, so no extra plumbing is needed to keep it out of those two files.
    /// No-ops (doesn't even serialize <paramref name="request"/>/<paramref name="response"/>) when the
    /// toggle is off, so this is safe to call unconditionally from every endpoint.
    /// </summary>
    public void LogVerbosePayload(ILogger logger, string system, string endpoint, object? request, object? response)
    {
        if (!_verboseOptions.CurrentValue.Enabled)
        {
            return;
        }

        logger.LogInformation(
            "{System} {Endpoint}: verbose payload. Request: {@Request} Response: {@Response}",
            system, endpoint, request, response);
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
        string? expectedFailureReason = null,
        Func<TResult, IReadOnlyDictionary<string, object?>>? enrichContext = null)
    {
        TagActivity(system, endpoint, context);
        try
        {
            var result = await action();

            // Some callers (HISO: routing is only known after the mediator call resolves the
            // session, not before) need to add context entries (e.g. Routing.*) once the result is
            // in hand, so the CallLog readable line reflects it too - not just the response headers.
            var loggedContext = enrichContext is not null
                ? MergeContext(context, enrichContext(result))
                : context;

            if (isExpectedFailure is not null && isExpectedFailure(result))
            {
                RecordExpectedFailure(logger, system, endpoint, expectedFailureReason ?? "ExpectedFailure", loggedContext, result);
            }
            else
            {
                LogSuccess(logger, system, endpoint, loggedContext, result);
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
            LogSuccess(logger, system, endpoint, context, result);
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
    public void RecordExpectedFailure(ILogger logger, string system, string endpoint, string reason, IReadOnlyDictionary<string, object?> context, object? response = null)
    {
        TagActivity(system, endpoint, context);
        logger.LogWarning(
            "{System} {Endpoint}: expected failure ({Reason}). Context: {@Context}",
            system, endpoint, reason, context);
        EmitCallLog(logger, system, endpoint, succeeded: false, reason, context);
        LogVerbosePayload(logger, system, endpoint, context, response);

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
        EmitCallLog(logger, system, endpoint, succeeded: false, ex.Message, context);
        // The exception itself is already fully captured by the structured `ex` param on the LogError
        // call above (stack trace, message, type) - no separate verbose-payload line needed here.

        Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
        Activity.Current?.AddException(ex);

        _telemetry.RecordLegacyEndpointError(system, endpoint, ex.GetType().Name);
    }

    private static IReadOnlyDictionary<string, object?> MergeContext(IReadOnlyDictionary<string, object?> baseContext, IReadOnlyDictionary<string, object?> extra)
    {
        var merged = new Dictionary<string, object?>(baseContext);
        foreach (var (key, value) in extra)
        {
            merged[key] = value;
        }

        return merged;
    }

    private void LogSuccess(ILogger logger, string system, string endpoint, IReadOnlyDictionary<string, object?> context, object? response = null)
    {
        logger.LogInformation(
            "{System} {Endpoint} succeeded. Context: {@Context}",
            system, endpoint, context);
        EmitCallLog(logger, system, endpoint, succeeded: true, failureReason: null, context);
        LogVerbosePayload(logger, system, endpoint, context, response);
    }

    /// <summary>
    /// FR-12/FR-6 (logging overhaul, 2026-07-29): one plain-English line per call, automatically for
    /// every legacy-compat endpoint that goes through this observer (not hand-wired per controller).
    /// Message template deliberately contains both "{System}" (so the per-system file router in
    /// Program.cs can filter on it) and the literal text "[CallLog]" (so the readable-file sub-logger
    /// can filter on that, separate from the full technical/errors files). Routing info is only as
    /// complete as whatever the caller has already put in <paramref name="context"/> under the
    /// "Routing.*" keys (see <see cref="RoutingHeaderWriter"/>/the FR-12 pilot controllers) - endpoints
    /// that haven't had routing resolution added yet simply log without a DB server/environment, never
    /// invented.
    /// </summary>
    private static void EmitCallLog(ILogger logger, string system, string endpoint, bool succeeded, string? failureReason, IReadOnlyDictionary<string, object?> context)
    {
        var patientId = context.TryGetValue("PatientId", out var p) ? p?.ToString() : null;
        var encounterId = context.TryGetValue("EncounterId", out var e) ? e?.ToString() : null;

        CallRoutingInfo? routing = null;
        if (context.TryGetValue("Routing.DbServerHost", out var dbHost) && dbHost is not null)
        {
            context.TryGetValue("Routing.Environment", out var env);
            context.TryGetValue("Routing.DbName", out var dbName);
            context.TryGetValue("Routing.PracticeId", out var practiceId);
            routing = new CallRoutingInfo(system, practiceId?.ToString() ?? "-", env?.ToString() ?? "-", dbHost.ToString()!, dbName?.ToString() ?? "-");
        }

        // Optional: a caller that already knows the exact field names returned (e.g. via
        // RoutingSummaryFormatter.GetNonNullPropertyNames on its own result type) can pass them
        // through context["FieldsReturned"] for a richer line; generic callers simply omit it.
        var fieldsReturned = context.TryGetValue("FieldsReturned", out var fields) ? fields as IReadOnlyCollection<string> : null;
        var summary = RoutingSummaryFormatter.BuildCallLogLine(system, endpoint, patientId, encounterId, succeeded, failureReason, fieldsReturned, routing);
        logger.LogInformation("{System} [CallLog] {CallSummary}", system, summary);
    }

    /// <summary>
    /// For controllers that don't route their success path through <see cref="ObserveAsync{TResult}"/>/
    /// <see cref="ObserveSwallowedAsync"/> at all (e.g. `KaroCompatController`, which only calls this
    /// class on the failure branch) - lets a success path still tag the trace span with real business
    /// context, without implying a warning/error the way `RecordExpectedFailure` would.
    /// </summary>
    public void Tag(ILogger logger, string system, string endpoint, IReadOnlyDictionary<string, object?> context, object? response = null)
    {
        TagActivity(system, endpoint, context);
        EmitCallLog(logger, system, endpoint, succeeded: true, failureReason: null, context);
        LogVerbosePayload(logger, system, endpoint, context, response);
    }

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
