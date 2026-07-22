using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.NextOfKin;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Canonical resource (2026-07-23). No KARO method - KARO has no real next-of-kin operation.</summary>
public interface ICanonicalNextOfKinRepository
{
    Task<IReadOnlyList<NextOfKinCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<NextOfKinCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
