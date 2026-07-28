namespace HekCoreApi.Api.Middleware;

/// <summary>
/// Decorates every response with baseline OWASP-recommended security headers. HSTS is only added
/// outside Development, since it only makes sense over HTTPS and would otherwise get in the way of
/// local plain-HTTP dev workflows.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _includeHsts;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _includeHsts = !environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";

            if (_includeHsts)
            {
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
