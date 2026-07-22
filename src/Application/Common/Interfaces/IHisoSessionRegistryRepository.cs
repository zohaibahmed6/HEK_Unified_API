using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Read-path over the central HISO session registry (HekTenantRegistry.HisoSessions) - a SessionGuid's
/// full routing details (own DbServerHost/DbName, mirroring Practices' shape). ERMS/KARO routing
/// continues through ITenantRegistryService/Practices instead - this repository is HISO-only.
/// </summary>
public interface IHisoSessionRegistryRepository
{
    Task<HisoSessionRoute?> FindAsync(Guid sessionGuid, CancellationToken ct = default);

    /// <summary>
    /// Looks up the most recent active HisoSessions row for a practice, by PracticeId rather than
    /// SessionGuid - the routing source for every other HISO repository (concept dictionary/executor,
    /// ACC45, tasks), replacing Practices for HISO entirely.
    /// </summary>
    Task<HisoSessionRoute?> FindByPracticeIdAsync(string practiceId, CancellationToken ct = default);
}
