using HekCoreApi.Contracts.Tasks;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>HISO-BR-19/20: subject = resolved SNOMED/Read-code concept name + free-text description; status via configured status IDs.</summary>
public interface ITasksRepository
{
    Task<TaskResult> CreateAsync(int patientId, string practiceId, TaskInput input, CancellationToken ct = default);
}
