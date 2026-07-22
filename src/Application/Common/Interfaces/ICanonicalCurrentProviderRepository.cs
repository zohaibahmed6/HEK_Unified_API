using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Providers;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Eighth canonical resource (2026-07-23), same per-source-method pattern as <see cref="IDemographicsRepository"/>.</summary>
public interface ICanonicalCurrentProviderRepository
{
    Task<CurrentProviderCanonical?> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<CurrentProviderCanonical?> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);

    Task<CurrentProviderCanonical?> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
