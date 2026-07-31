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
/// For KARO specifically, the operation segment itself also needs translating (see
/// <see cref="KaroOperationNameMap"/>) - 2026-07-30 fix, real external operation names don't match
/// KaroCompatController's internal route names the way ERMS/COL's do.
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

    // KARO's real external operation names (`GetDemographics`, `SaveDocument`, ...) don't match
    // KaroCompatController's short internal route names (`demographics`, `document`, ...) - unlike
    // ERMS/COL, where the internal route name is spelled identically to the real external one, so a
    // prefix-only swap was never enough here. Confirmed live during the 2026-07-30 crosscheck: every
    // KARO operation except ping/authenticate (whose names happen to match either way) 404'd at its
    // real external URL despite working at its internal route. This map translates the operation
    // segment itself, not just the prefix.
    private static readonly Dictionary<string, string> KaroOperationNameMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GetClinicalNotes"] = "clinicalnotes",
            ["SaveClinicalNotes"] = "clinicalnotes",
            ["GetConditions"] = "conditions",
            ["SaveCondition"] = "conditions",
            ["GetDemographics"] = "demographics",
            ["GetDocuments"] = "documents",
            ["GetEncounterSummary"] = "encountersummary",
            ["GetLabResults"] = "labresults",
            ["GetMedications"] = "medications",
            ["GetObservations"] = "observations",
            ["SaveObservations"] = "observations",
            ["GetProvider"] = "provider",
            ["GetRecallCategories"] = "recallcategories",
            ["GetRecalls"] = "recalls",
            ["SaveRecall"] = "recalls",
            ["GetScreeningCodes"] = "screeningcodes",
            ["SaveScreeningCode"] = "screeningcodes",
            ["GetPatientAttachment"] = "patientattachment",
            ["SaveSummary"] = "summary",
            ["SaveDocument"] = "document",
            ["SaveInvoice"] = "invoice",
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

        // ERMS and COL share the same real host (`southerms.indici.nz`) - COL is a second controller
        // living inside the same legacy ERMS web project, not a separate host. So a host can match
        // more than one rule; the external path prefix (`/api` vs `/COL`) is what actually disambiguates
        // which system a given request belongs to - picking only the first host match (previous
        // behavior) silently dropped COL's rewrite whenever ERMS's rule matched first.
        foreach (var rule in _options.Rules)
        {
            if (string.IsNullOrEmpty(rule.HostContains) || !host.Contains(rule.HostContains, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!PrefixesBySystem.TryGetValue(rule.System, out var prefixes))
            {
                continue;
            }

            if (path.StartsWith(prefixes.ExternalPrefix, StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith(prefixes.InternalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var remainder = path[prefixes.ExternalPrefix.Length..];

                if (rule.System.Equals("Karo", StringComparison.OrdinalIgnoreCase) && remainder.Length > 1)
                {
                    var slashIndex = remainder.IndexOf('/', 1);
                    var operationSegment = slashIndex >= 0 ? remainder[1..slashIndex] : remainder[1..];
                    var restOfPath = slashIndex >= 0 ? remainder[slashIndex..] : string.Empty;

                    if (KaroOperationNameMap.TryGetValue(operationSegment, out var internalOperation))
                    {
                        remainder = "/" + internalOperation + restOfPath;
                    }
                }

                context.Request.Path = prefixes.InternalPrefix + remainder;
                break;
            }
        }

        return _next(context);
    }
}
