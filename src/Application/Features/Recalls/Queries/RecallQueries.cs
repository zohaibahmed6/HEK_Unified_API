using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Recalls;
using MediatR;

namespace HekCoreApi.Application.Features.Recalls.Queries;

public sealed record GetRecallCategoriesQuery(string PracticeId, string? Group) : IRequest<IReadOnlyList<RecallCategory>>;

public sealed class GetRecallCategoriesQueryHandler : IRequestHandler<GetRecallCategoriesQuery, IReadOnlyList<RecallCategory>>
{
    private readonly IRecallsRepository _repository;
    public GetRecallCategoriesQueryHandler(IRecallsRepository repository) => _repository = repository;
    public Task<IReadOnlyList<RecallCategory>> Handle(GetRecallCategoriesQuery request, CancellationToken cancellationToken) =>
        _repository.GetCategoriesAsync(request.PracticeId, request.Group, cancellationToken);
}

public sealed record GetRecallsForPatientQuery(int PatientId, string PracticeId) : IRequest<IReadOnlyList<Recall>>;

public sealed class GetRecallsForPatientQueryHandler : IRequestHandler<GetRecallsForPatientQuery, IReadOnlyList<Recall>>
{
    private readonly IRecallsRepository _repository;
    public GetRecallsForPatientQueryHandler(IRecallsRepository repository) => _repository = repository;
    public Task<IReadOnlyList<Recall>> Handle(GetRecallsForPatientQuery request, CancellationToken cancellationToken) =>
        _repository.GetForPatientAsync(request.PatientId, request.PracticeId, cancellationToken);
}
