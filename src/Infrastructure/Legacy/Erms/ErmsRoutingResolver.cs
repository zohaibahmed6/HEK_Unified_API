using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <inheritdoc cref="IErmsRoutingResolver"/>
public sealed class ErmsRoutingResolver : IErmsRoutingResolver
{
    public RoutingContext Resolve(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return RoutingContext.FromPracticeId(RoutingContext.Unscoped, OriginScope.Erms);
        }

        var segments = encounterId.Split(new[] { "__" }, StringSplitOptions.None);

        var practiceId = segments.Length > 1 ? segments[1] : RoutingContext.Unscoped;
        var practiceCode = segments.Length > 2 ? segments[2] : RoutingContext.Unscoped;
        var environment = RoutingContext.Unscoped;

        // Legacy quirk (ermsapi APIController.cs:71-74's `practiceid = "_" + splitEncounter[3]`
        // overwrite, same real pattern as KARO's HSSDA.cs:813 ConnIndiciDB+practiceid lookup): a 4th
        // segment overwrites the practice context entirely - reproduced here as routing by
        // environment alone when a 4th segment is present, matching KaroRoutingResolver's identical fix.
        if (segments.Length > 3)
        {
            practiceId = RoutingContext.Unscoped;
            practiceCode = RoutingContext.Unscoped;
            environment = segments[3];
        }

        return new RoutingContext(practiceId, practiceCode, environment, OriginScope.Erms);
    }
}
