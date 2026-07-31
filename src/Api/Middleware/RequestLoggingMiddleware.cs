namespace HekCoreApi.Api.Middleware;

/// <summary>
/// Safety-net logger for EVERY request, including ones that never reach a controller (404s, CORS
/// rejections, auth failures) - those never carry a "System" property, so they'd otherwise be
/// invisible to the per-system readable/technical/errors files (and Microsoft.AspNetCore's own
/// request logging is suppressed to Warning in appsettings.json). Writes to logs/requests-.log so
/// a call that silently "goes nowhere" always leaves a trace with the exact path/status it hit.
/// Registered first in the pipeline, before routing/CORS/auth.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = DateTime.UtcNow;
        await _next(context);
        var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;

        var statusCode = context.Response.StatusCode;
        var message = "{Method} {Path} -> {StatusCode} ({ElapsedMs:F0}ms)";
        if (statusCode >= 400)
        {
            _logger.LogWarning(message, context.Request.Method, context.Request.Path.Value, statusCode, elapsedMs);
        }
        else
        {
            _logger.LogInformation(message, context.Request.Method, context.Request.Path.Value, statusCode, elapsedMs);
        }
    }
}
