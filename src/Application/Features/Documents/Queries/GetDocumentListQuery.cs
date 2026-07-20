using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Documents;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Documents.Queries;

public sealed record GetDocumentListQuery(
    OriginScope Origin, int PatientId, string PracticeId, string? Direction, string? ContentType,
    string? ReferenceId, string? Subject, DateOnly? SinceDate, DateOnly? UntilDate, string? SortOrder) : IRequest<IReadOnlyList<DocumentSummary>>;

public sealed class GetDocumentListQueryHandler : IRequestHandler<GetDocumentListQuery, IReadOnlyList<DocumentSummary>>
{
    private readonly IDocumentsRepository _repository;

    public GetDocumentListQueryHandler(IDocumentsRepository repository) => _repository = repository;

    public Task<IReadOnlyList<DocumentSummary>> Handle(GetDocumentListQuery request, CancellationToken cancellationToken) =>
        _repository.GetListAsync(request.Origin, request.PatientId, request.PracticeId, request.Direction, request.ContentType, request.ReferenceId, request.Subject, request.SinceDate, request.UntilDate, request.SortOrder, cancellationToken);
}
