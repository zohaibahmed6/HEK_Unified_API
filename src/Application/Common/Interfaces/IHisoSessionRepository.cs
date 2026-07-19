using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Read-only access to HISO's existing, unchanged [Appointment].[tblHealthLinkSession] mechanism
/// (ADR-004/ADR-007). No schema changes (ADR-010).
/// </summary>
public interface IHisoSessionRepository
{
    /// <summary>
    /// Resolves a SessionGUID via the given server address (ADR-007: routing decided by which
    /// address was called). Returns null if the GUID does not resolve on that server.
    /// </summary>
    Task<HisoSessionContext?> FindBySessionGuidAsync(Guid sessionGuid, string serverAddress, CancellationToken ct = default);
}
