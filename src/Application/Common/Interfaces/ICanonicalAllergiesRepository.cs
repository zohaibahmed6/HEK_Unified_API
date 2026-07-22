using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Allergies;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Seventh canonical resource (2026-07-23). No KARO method - KARO has no real allergies operation (confirmed gap).</summary>
public interface ICanonicalAllergiesRepository
{
    Task<IReadOnlyList<AllergyCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<AllergyCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
