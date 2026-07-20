using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Documents;
using MediatR;

namespace HekCoreApi.Application.Features.Documents.Queries;

public sealed record GetDocumentDetailQuery(string PracticeId, string DocumentId) : IRequest<Document?>;

public sealed class GetDocumentDetailQueryHandler : IRequestHandler<GetDocumentDetailQuery, Document?>
{
    private readonly IDocumentsRepository _repository;

    public GetDocumentDetailQueryHandler(IDocumentsRepository repository) => _repository = repository;

    public Task<Document?> Handle(GetDocumentDetailQuery request, CancellationToken cancellationToken) =>
        _repository.GetDetailAsync(request.PracticeId, request.DocumentId, cancellationToken);
}
