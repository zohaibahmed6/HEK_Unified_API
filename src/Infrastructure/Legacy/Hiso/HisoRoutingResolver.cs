using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <inheritdoc cref="IHisoRoutingResolver"/>
public sealed class HisoRoutingResolver : IHisoRoutingResolver
{
    public RoutingContext Resolve(HisoSessionContext session) =>
        RoutingContext.FromPracticeId(session.PracticeId, OriginScope.Hiso);
}
