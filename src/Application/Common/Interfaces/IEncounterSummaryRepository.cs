using HekCoreApi.Contracts.EncounterSummary;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// KARO-unique (KARO-BR-10/11). GetSummaryAsync/SaveSummaryAsync are the real, working
/// implementations replacing legacy GetEncounterSummary's hardcoded mock data (KARO-BR-09) - per
/// stakeholder Decision 5, not a reproduction of the broken behavior.
/// </summary>
public interface IEncounterSummaryRepository
{
    Task<TemplateSchema?> GetTemplateSchemaAsync(string practiceId, string identifier, CancellationToken ct = default);

    Task<EncounterSummaryData?> GetSummaryAsync(int patientId, int encounterId, string practiceId, string identifier, CancellationToken ct = default);

    Task<EncounterSummaryData> SaveSummaryAsync(int patientId, int encounterId, string practiceId, EncounterSummaryInput input, CancellationToken ct = default);
}
