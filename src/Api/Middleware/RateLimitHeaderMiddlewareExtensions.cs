namespace HekCoreApi.Api.Middleware;

public static class RateLimitHeaderMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimitHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitHeaderMiddleware>();
    }
}
