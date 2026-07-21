namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from legacy-reference/Hiso/Task.cs (`processTask`/`AddTask`). Subject is resolved via a
/// real SNOMED/Read-code lookup (`Billing.usptblConcept_GetByCode`), then saved via
/// `Task.uspAddTaskExternal`.
/// </summary>
public interface IHisoTaskRepository
{
    Task<bool> AddTaskAsync(HisoTaskInput input, string practiceId, CancellationToken ct = default);
}

/// <summary>Parsed from `processAction`'s `actionContainer` XML (`code`, `taskDescription`, `dueDate`, `complete`) plus session context needed by `Task.uspAddTaskExternal`.</summary>
public sealed record HisoTaskInput(
    string Code,
    string? Description,
    DateOnly DueDate,
    bool Complete,
    string ProviderId,
    string PatientId,
    string? Acc45Id,
    string? UpdatedBy);
