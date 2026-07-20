namespace HekCoreApi.Contracts.Documents;

public sealed record Document(string DocumentId, int PatientId, string Direction, string ContentType, DateTimeOffset CreatedAt, string? Subject, string? ReferenceId, string Content)
{
    public static Document FromSummary(DocumentSummary summary, string content) =>
        new(summary.DocumentId, summary.PatientId, summary.Direction, summary.ContentType, summary.CreatedAt, summary.Subject, summary.ReferenceId, content);
}
