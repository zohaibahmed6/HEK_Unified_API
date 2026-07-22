using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.LabResults;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Fifth canonical resource (2026-07-23), same per-source-method pattern as <see cref="IDemographicsRepository"/>.</summary>
public interface ICanonicalLabResultsRepository
{
    Task<IReadOnlyList<LabResultCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<LabResultCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);

    Task<IReadOnlyList<LabResultCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
