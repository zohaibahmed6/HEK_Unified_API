using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Documents;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Third canonical resource (2026-07-22), same per-source-method pattern as <see cref="IDemographicsRepository"/>/<see cref="ICanonicalConditionsRepository"/>.</summary>
public interface ICanonicalDocumentsRepository
{
    Task<IReadOnlyList<DocumentCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
