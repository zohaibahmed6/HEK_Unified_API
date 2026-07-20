using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Providers;
using MediatR;

namespace HekCoreApi.Application.Features.Providers.Queries;

public sealed record GetProvidersQuery(string PracticeId, string? PracticeLocationId) : IRequest<IReadOnlyList<Provider>>;

public sealed class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, IReadOnlyList<Provider>>
{
    private readonly IProvidersRepository _repository;
    public GetProvidersQueryHandler(IProvidersRepository repository) => _repository = repository;
    public Task<IReadOnlyList<Provider>> Handle(GetProvidersQuery request, CancellationToken cancellationToken) =>
        _repository.GetAsync(request.PracticeId, request.PracticeLocationId, cancellationToken);
}
