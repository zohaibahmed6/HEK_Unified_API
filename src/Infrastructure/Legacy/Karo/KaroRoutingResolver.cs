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
        var environment = segments.Length > 3 ? segments[3] : RoutingContext.Unscoped;

        return new RoutingContext(practiceId, practiceCode, environment, OriginScope.Karo);
    }
}
