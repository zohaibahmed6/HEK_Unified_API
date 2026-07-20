using HekCoreApi.Contracts.Recalls;

namespace HekCoreApi.Application.Common.Interfaces;

public interface IRecallsRepository
{
    Task<IReadOnlyList<RecallCategory>> GetCategoriesAsync(string practiceId, string? group, CancellationToken ct = default);

    Task<IReadOnlyList<Recall>> GetForPatientAsync(int patientId, string practiceId, CancellationToken ct = default);

    /// <summary>Empty categoryId defaults per group at the stored-procedure level (KARO-BR-22) - passed through as-is, not defaulted client-side.</summary>
    Task<Recall> SaveAsync(int patientId, string practiceId, RecallInput input, CancellationToken ct = default);
}
