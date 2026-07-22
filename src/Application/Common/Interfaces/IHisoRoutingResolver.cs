using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Builds a <see cref="RoutingContext"/> from HISO's own real identifier - the already-resolved
/// session context (ADR-004/ADR-007's unmodified SessionGUID lookup, via
/// <c>ResolveHisoSessionQuery</c>/<c>HisoServerAddressMapOptions</c>), not a modified session-key
/// format. HISO has no real PracticeCode/Environment segmentation today, so both are
/// <see cref="RoutingContext.Unscoped"/> - kept as its own resolver (not a shared base class) so a
/// future real HISO routing signal only changes this one type.
/// </summary>
public interface IHisoRoutingResolver
{
    RoutingContext Resolve(HisoSessionContext session);
}
