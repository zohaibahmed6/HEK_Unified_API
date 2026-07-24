using HekCoreApi.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Api.Middleware;

/// <summary>
/// v1.1 spec follow-through, Step 2 (see PROJECT_STATUS.md / hek_analysis/v1.1-full-plan.md). Rewrites
/// the real external legacy path shapes (`/api/{OperationName}` for KARO and ERMS, `/COL/{OperationName}`
/// for COL) onto this hub's already-verified internal routes (`/karo`, `/erms`, `/erms/col`), chosen by
/// which real hostname the request arrived on (<see cref="LegacyHostRoutingOptions"/>) - since KARO and
/// ERMS share the identical `/api/...` shape but are different systems. Runs before MVC routing, so no
/// controller code changes; a request whose Host matches no configured rule falls through with its path
/// untouched, keeping the original `/karo`/`/erms`/`/erms/col` paths working for local dev/testing.
/// </summary>
public sealed class LegacyHostRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LegacyHostRoutingOptions _options;

    private static readonly Dictionary<string, (string ExternalPrefix, string InternalPrefix)> PrefixesBySystem =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Karo"] = ("/api", "/karo"),
            ["Erms"] = ("/api", "/erms"),
            ["Col"] = ("/COL", "/erms/col"),
        };

    public LegacyHostRoutingMiddleware(RequestDelegate next, IOptions<LegacyHostRoutingOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var path = context.Request.Path.Value ?? string.Empty;

        var matchedSystem = _options.Rules.FirstOrDefault(r =>
            !string.IsNullOrEmpty(r.HostContains) && host.Contains(r.HostContains, StringComparison.OrdinalIgnoreCase))?.System;

        if (matchedSystem is not null && PrefixesBySystem.TryGetValue(matchedSystem, out var prefixes) &&
            path.StartsWith(prefixes.ExternalPrefix, StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith(prefixes.InternalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = prefixes.InternalPrefix + path[prefixes.ExternalPrefix.Length..];
        }

        return _next(context);
    }
}
