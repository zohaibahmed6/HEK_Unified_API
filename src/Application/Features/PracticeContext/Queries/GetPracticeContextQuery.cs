using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.PracticeContext;
using MediatR;

namespace HekCoreApi.Application.Features.PracticeContext.Queries;

public sealed record GetPracticeContextQuery(string PracticeId) : IRequest<PracticeSessionContext?>;

public sealed class GetPracticeContextQueryHandler : IRequestHandler<GetPracticeContextQuery, PracticeSessionContext?>
{
    private readonly IPracticeContextRepository _repository;
    public GetPracticeContextQueryHandler(IPracticeContextRepository repository) => _repository = repository;
    public Task<PracticeSessionContext?> Handle(GetPracticeContextQuery request, CancellationToken cancellationToken) =>
        _repository.GetAsync(request.PracticeId, cancellationToken);
}
