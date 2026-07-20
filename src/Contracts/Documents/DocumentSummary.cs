namespace HekCoreApi.Contracts.Documents;

public sealed record DocumentSummary(string DocumentId, int PatientId, string Direction, string ContentType, DateTimeOffset CreatedAt, string? Subject, string? ReferenceId);
