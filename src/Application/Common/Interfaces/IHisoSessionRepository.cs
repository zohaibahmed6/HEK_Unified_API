using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Read-only access to HISO's existing, unchanged [Appointment].[tblHealthLinkSession] mechanism
/// (ADR-004). No schema changes (ADR-010).
/// </summary>
public interface IHisoSessionRepository
{
    /// <summary>
    /// Resolves a SessionGUID against the given, already-resolved connection string. Routing
    /// (which connection to use) is decided upstream, via the central HISO session registry
    /// (HekTenantRegistry.HisoSessions) + the tenant registry - not by Host header (superseded
    /// 2026-07-22, see ResolveHisoSessionQueryHandler). Returns null if the GUID isn't found there.
    /// </summary>
    Task<HisoSessionContext?> FindBySessionGuidAsync(Guid sessionGuid, string connectionString, CancellationToken ct = default);
}
