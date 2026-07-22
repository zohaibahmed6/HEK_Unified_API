using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Smoking;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Canonical resource (2026-07-23). No KARO method - KARO has no real smoking-status operation.</summary>
public interface ICanonicalSmokingStatusRepository
{
    Task<IReadOnlyList<SmokingStatusCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<SmokingStatusCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
