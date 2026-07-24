namespace HekCoreApi.Api.Middleware;

public static class LegacyHostRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseLegacyHostRouting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LegacyHostRoutingMiddleware>();
    }
}
