using HekCoreApi.Application.Common.Idempotency;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Documents;
using MediatR;

namespace HekCoreApi.Application.Features.Documents.Commands;

public sealed record SaveDocumentCommand(int PatientId, string PracticeId, DocumentInput Input, string? IdempotencyKey) : IRequest<IdempotencyOutcome<DocumentSummary>>;

public sealed class SaveDocumentCommandHandler : IRequestHandler<SaveDocumentCommand, IdempotencyOutcome<DocumentSummary>>
{
    private readonly IDocumentsRepository _repository;
    private readonly IIdempotencyStore _idempotencyStore;

    public SaveDocumentCommandHandler(IDocumentsRepository repository, IIdempotencyStore idempotencyStore)
    {
        _repository = repository;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<IdempotencyOutcome<DocumentSummary>> Handle(SaveDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var key = IdempotencyKeyBuilder.Build("documents", request.PracticeId, request.PatientId.ToString(), null, request.IdempotencyKey);
            var cached = await _idempotencyStore.TryGetAsync<DocumentSummary>(key, cancellationToken);
            if (cached is not null)
            {
                return new IdempotencyOutcome<DocumentSummary>(true, cached);
            }
        }

        // ERMS-BR-19-style natural key: same referenceId already filed is a non-error duplicate.
        if (!string.IsNullOrEmpty(request.Input.ReferenceId))
        {
            var existing = await _repository.FindByReferenceIdAsync(request.PracticeId, request.Input.ReferenceId, cancellationToken);
            if (existing is not null)
            {
                return new IdempotencyOutcome<DocumentSummary>(true, existing);
            }
        }

        var created = await _repository.SaveAsync(request.PatientId, request.PracticeId, request.Input, cancellationToken);

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var key = IdempotencyKeyBuilder.Build("documents", request.PracticeId, request.PatientId.ToString(), null, request.IdempotencyKey);
            await _idempotencyStore.SetAsync(key, created, cancellationToken);
        }

        return new IdempotencyOutcome<DocumentSummary>(false, created);
    }
}
