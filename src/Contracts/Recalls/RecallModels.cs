namespace HekCoreApi.Contracts.Recalls;

public sealed record RecallCategory(string CategoryId, string Name);

public sealed record Recall(string RecallId, int PatientId, string? CategoryId, DateOnly DueDate);

public sealed record RecallInput(string? CategoryId, DateOnly DueDate);
