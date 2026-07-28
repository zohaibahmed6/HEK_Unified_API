using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// ERMS DMS connection routing. Real legacy Web.config confirms the DMS connection (e.g.
/// `ConectionStringPMS_NZ_DMS`) sits on the SAME server, with the SAME credentials, as the practice's
/// primary connection (`ConectionStringPMS_NZ`) - only the `Initial Catalog` differs (`DMS_PMS` vs
/// the primary practice database). So this resolves through the same tenant-registry route as
/// <see cref="IErmsPracticeConnectionResolver"/> and only swaps the database name, instead of requiring
/// its own separate per-practice secret to be configured. ERMS-owned twin of
/// <see cref="IKaroDmsConnectionResolver"/> - kept separate for module isolation.
/// </summary>
public interface IErmsDmsConnectionResolver
{
    Task<string> ResolveAsync(RoutingContext routingContext, CancellationToken ct = default);
}
