using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Conditions;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>One canonical implementation, routed by originScope (HISO ACC45 diagnosis builders / KARO GetConditions / ERMS GetClassifications / COL GetDiagnosisData).</summary>
public interface IConditionsRepository
{
    Task<IReadOnlyList<Condition>> GetAsync(OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession, CancellationToken ct = default);

    /// <summary>Natural key per KARO-BR-12: same diagnosis code for the same appointment. Null if none exists yet.</summary>
    Task<Condition?> FindByNaturalKeyAsync(int encounterId, string practiceId, string diagnosisCode, CancellationToken ct = default);

    Task<Condition> SaveAsync(int patientId, int encounterId, string practiceId, ConditionInput input, CancellationToken ct = default);
}
