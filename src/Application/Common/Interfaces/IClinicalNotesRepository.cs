using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.ClinicalNotes;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// One canonical implementation, routed internally by originScope (Contract Design doc Section 8
/// Decision 1 - "one canonical implementation per capability"). GET: HISO getData / KARO
/// GetClinicalNotes / ERMS GetConsultNotes (default 24-month lookback if no date range, ERMS-BR-05/
/// FR-CLIN-02). POST: KARO SaveClinicalNotes only - no HISO/ERMS save source is documented.
/// </summary>
public interface IClinicalNotesRepository
{
    Task<IReadOnlyList<ClinicalNote>> GetAsync(
        OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession,
        DateOnly? sinceDate, DateOnly? untilDate, string? sortOrder, CancellationToken ct = default);

    Task<ClinicalNote> SaveAsync(int patientId, int encounterId, string practiceId, string content, CancellationToken ct = default);
}
