using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <inheritdoc cref="IKaroRoutingResolver"/>
public sealed class KaroRoutingResolver : IKaroRoutingResolver
{
    public RoutingContext Resolve(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return RoutingContext.FromPracticeId(RoutingContext.Unscoped, OriginScope.Karo);
        }

        var segments = encounterId.Split(new[] { "__" }, StringSplitOptions.None);

        var practiceId = segments.Length > 1 ? segments[1] : RoutingContext.Unscoped;
        var practiceCode = segments.Length > 2 ? segments[2] : RoutingContext.Unscoped;
        var environment = RoutingContext.Unscoped;

        // Legacy quirk (APIController.cs:55-58, HSSDA.cs:813's `ConnIndiciDB{practiceid}` connection-
        // string lookup): a 4th segment overwrites the practice context entirely in the real system -
        // the real connection key collapses to just "ConnIndiciDB_{4th segment}", discarding
        // practiceId/practiceCode. Reproduced here as routing by environment alone when a 4th segment
        // is present, so the tenant registry lookup mirrors legacy's real precedence instead of
        // matching all three fields independently.
        if (segments.Length > 3)
        {
            practiceId = RoutingContext.Unscoped;
            practiceCode = RoutingContext.Unscoped;
            environment = segments[3];
        }

        return new RoutingContext(practiceId, practiceCode, environment, OriginScope.Karo);
    }
}
