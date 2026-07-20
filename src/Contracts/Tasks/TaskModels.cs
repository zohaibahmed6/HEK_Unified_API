namespace HekCoreApi.Contracts.Tasks;

public sealed record TaskInput(string ConceptCode, string Description, string? Status);

public sealed record TaskResult(string TaskId, string Subject, string Status);
