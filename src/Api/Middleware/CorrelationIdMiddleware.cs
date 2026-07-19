using Serilog.Context;

namespace HekCoreApi.Api.Middleware;

/// <summary>
/// Reads or generates an X-Correlation-ID per request, pushes it into Serilog's LogContext (so
/// every log line for this request is correlated) and into HttpContext.Items so the RFC 7807
/// exception-handling middleware can surface the same identifier as the client-visible traceId.
/// Registered early in the pipeline, before auth/routing.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
