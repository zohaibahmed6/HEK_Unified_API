using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Demographics;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Demographics stays four separate legacy-shaped lookups, never merged (Contract Design doc
/// Section 4.2, Section 8 Decision 6) - one method per legacy source.
/// </summary>
public interface IDemographicsRepository
{
    Task<DemographicsHiso?> GetHisoAsync(int patientId, HealthLinkSession session, CancellationToken ct = default);

    Task<DemographicsKaro?> GetKaroAsync(int patientId, RoutingContext routing, CancellationToken ct = default);

    Task<DemographicsErms?> GetErmsAsync(int patientId, RoutingContext routing, CancellationToken ct = default);

    Task<DemographicsCol?> GetColAsync(int patientId, string practiceId, CancellationToken ct = default);
}
