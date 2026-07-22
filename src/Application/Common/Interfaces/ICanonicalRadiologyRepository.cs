using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Radiology;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Canonical resource (2026-07-23). No KARO method - KARO has no real radiology operation.</summary>
public interface ICanonicalRadiologyRepository
{
    Task<IReadOnlyList<RadiologyReportCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<RadiologyReportCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
