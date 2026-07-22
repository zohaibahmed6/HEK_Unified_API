using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Medications;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Sixth canonical resource (2026-07-23), same per-source-method pattern as <see cref="IDemographicsRepository"/>.</summary>
public interface ICanonicalMedicationsRepository
{
    Task<IReadOnlyList<MedicationCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<MedicationCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);

    Task<IReadOnlyList<MedicationCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
