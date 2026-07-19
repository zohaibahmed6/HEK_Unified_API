using HekCoreApi.Api.Configuration;
using HekCoreApi.Contracts.RateLimiting;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Api.Middleware;

/// <summary>
/// Decorates every response with the standard rate-limit headers (Contract Design doc Section 14 /
/// OpenAPI spec) using the Contracts header-name constants. Runs after the built-in rate limiter so
/// a 429 already carries Retry-After from ASP.NET Core's own limiter; this middleware adds the
/// remaining descriptive headers on all responses, using config-driven placeholder values while
/// RateLimitOptions.Enabled is false.
/// </summary>
public sealed class RateLimitHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;

    public RateLimitHeaderMiddleware(RequestDelegate next, IOptions<RateLimitOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Enabled)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[RateLimitHeaderNames.Limit] = _options.PermitLimit.ToString();
                context.Response.Headers[RateLimitHeaderNames.Reset] = _options.WindowSeconds.ToString();
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
