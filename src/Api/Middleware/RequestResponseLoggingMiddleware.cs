using System.Text;
using HekCoreApi.Application.Common.Options;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace HekCoreApi.Api.Middleware;

/// <summary>
/// Tags every request with an ambient "System" LogContext property (so ANY log line written
/// anywhere during this request - including deep Infrastructure-layer code such as
/// <see cref="HekCoreApi.Infrastructure.Legacy.LegacyDbExecutor"/> that never explicitly passes
/// "{System}" - still routes to the correct per-system log file, see Program.cs's per-system Serilog
/// sinks) and captures the full raw request/response body in one log line per call. Added 2026-07-31
/// after confirming two real observability gaps: (1) several Infrastructure-layer log calls had no
/// "System" tag and were only ever visible via `docker logs`, never the per-system files; (2) the
/// only "request" data ever logged by write-op controllers was a hand-picked PatientId/EncounterId
/// pulled from the query string - which is empty for POST-body operations like KARO SaveInvoice, so
/// the real submitted fields (Name/Code/Fee/payee/...) were never captured anywhere. This middleware
/// closes both gaps in one place rather than requiring every controller/repository to opt in.
/// Registered early (with CorrelationIdMiddleware/RequestLoggingMiddleware), before the SOAP endpoint
/// and before LegacyHostRoutingMiddleware's path rewrite, so it sees both SOAP and REST traffic - for
/// real external REST calls (pre-rewrite, e.g. `/api/...`, `/COL/...`) the system is resolved the same
/// way LegacyHostRoutingMiddleware does (Host header against `LegacyHostRouting:Rules`); for
/// internal-route testing (`/karo`, `/erms`, `/erms/col`, `/hiso`) and the fixed SOAP path
/// (`/FormSessionService.svc`) it's resolved directly from the path.
/// </summary>
public sealed class RequestResponseLoggingMiddleware
{
    // Bodies larger than this are truncated in the log line (not dropped - the total real length is
    // still recorded) so one pathological call (e.g. a large HISO SOAP document) can't blow out the
    // per-system log files. Binary payloads (SQL parameter byte[] values) get the same size-only
    // treatment at the LegacyDbExecutor layer - see the comment there for the same reasoning.
    private const int MaxLoggedBodyLength = 50_000;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly LegacyHostRoutingOptions _hostRoutingOptions;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<LegacyHostRoutingOptions> hostRoutingOptions)
    {
        _next = next;
        _logger = logger;
        _hostRoutingOptions = hostRoutingOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var system = ResolveSystem(context);

        using (LogContext.PushProperty("System", system))
        {
            context.Request.EnableBuffering();
            var requestBody = await ReadRequestBodyAsync(context.Request);

            var originalResponseBody = context.Response.Body;
            await using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            try
            {
                await _next(context);
            }
            finally
            {
                responseBuffer.Position = 0;
                var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                _logger.LogInformation(
                    "{System} {Method} {Path} -> {StatusCode}. RequestBody: {RequestBody} ResponseBody: {ResponseBody}",
                    system,
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    Truncate(requestBody),
                    Truncate(responseBody));
            }
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            return string.Empty;
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static string Truncate(string value) =>
        value.Length > MaxLoggedBodyLength
            ? value[..MaxLoggedBodyLength] + $"...[truncated, {value.Length} total chars]"
            : value;

    private string ResolveSystem(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.Equals("/FormSessionService.svc", StringComparison.OrdinalIgnoreCase))
        {
            return "hiso-soap";
        }
        if (path.StartsWith("/erms/col", StringComparison.OrdinalIgnoreCase))
        {
            return "col";
        }
        if (path.StartsWith("/erms", StringComparison.OrdinalIgnoreCase))
        {
            return "erms";
        }
        if (path.StartsWith("/karo", StringComparison.OrdinalIgnoreCase))
        {
            return "karo";
        }
        if (path.StartsWith("/hiso", StringComparison.OrdinalIgnoreCase))
        {
            return "hiso";
        }

        // Real external pre-rewrite calls (/api/..., /COL/...) - disambiguate by Host, same rule
        // table LegacyHostRoutingMiddleware itself uses (runs later in the pipeline, after this).
        var host = context.Request.Host.Host;
        foreach (var rule in _hostRoutingOptions.Rules)
        {
            if (!string.IsNullOrEmpty(rule.HostContains) && host.Contains(rule.HostContains, StringComparison.OrdinalIgnoreCase))
            {
                return rule.System.ToLowerInvariant();
            }
        }

        return "unknown";
    }
}
