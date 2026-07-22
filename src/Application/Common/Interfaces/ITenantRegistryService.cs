using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Read-path over the tenant/practice registry (ADR-001). Block 1 ships only this resolution
/// service - the /admin/practices CRUD endpoints that write to the registry are Block 2 domain
/// group #16, not built here.
/// </summary>
public interface ITenantRegistryService
{
    /// <summary>
    /// Resolves which physical database server/name a practice's data lives on, or null if unknown.
    /// Matches on PracticeId alone - equivalent to <see cref="ResolveRouteAsync(RoutingContext, CancellationToken)"/>
    /// with <see cref="RoutingContext.Unscoped"/> PracticeCode/Environment.
    /// </summary>
    Task<PracticeRoute?> ResolveRouteAsync(string practiceId, CancellationToken ct = default);

    /// <summary>Resolves by the full (PracticeId, PracticeCode, Environment) triple (KARO/ERMS canonical routing).</summary>
    Task<PracticeRoute?> ResolveRouteAsync(RoutingContext context, CancellationToken ct = default);
}
