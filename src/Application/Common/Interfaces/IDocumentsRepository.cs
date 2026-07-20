using HekCoreApi.Contracts.Documents;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Interfaces;

public interface IDocumentsRepository
{
    Task<IReadOnlyList<DocumentSummary>> GetListAsync(
        OriginScope origin, int patientId, string practiceId, string? direction, string? contentType,
        string? referenceId, string? subject, DateOnly? sinceDate, DateOnly? untilDate, string? sortOrder, CancellationToken ct = default);

    Task<Document?> GetDetailAsync(string practiceId, string documentId, CancellationToken ct = default);

    /// <summary>Natural key per ERMS-BR-19's pattern (ReferenceID_DocumentID-style determinism) - null if no referenceId supplied or none found.</summary>
    Task<DocumentSummary?> FindByReferenceIdAsync(string practiceId, string referenceId, CancellationToken ct = default);

    Task<DocumentSummary> SaveAsync(int patientId, string practiceId, DocumentInput input, CancellationToken ct = default);
}
