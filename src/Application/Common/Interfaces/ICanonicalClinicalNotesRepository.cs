using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.ClinicalNotes;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Fourth canonical resource (2026-07-23), same per-source-method pattern as <see cref="IDemographicsRepository"/>.</summary>
public interface ICanonicalClinicalNotesRepository
{
    Task<IReadOnlyList<ClinicalNoteCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<ClinicalNoteCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);

    Task<IReadOnlyList<ClinicalNoteCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
