using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Providers;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Ninth canonical resource (2026-07-23). No KARO method - KARO has no real registered-practitioners operation.</summary>
public interface ICanonicalPractitionersRepository
{
    Task<IReadOnlyList<PractitionerCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<PractitionerCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
