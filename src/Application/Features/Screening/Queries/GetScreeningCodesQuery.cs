using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Screening;
using MediatR;

namespace HekCoreApi.Application.Features.Screening.Queries;

public sealed record GetScreeningCodesQuery(string PracticeId) : IRequest<IReadOnlyList<ScreeningCode>>;

public sealed class GetScreeningCodesQueryHandler : IRequestHandler<GetScreeningCodesQuery, IReadOnlyList<ScreeningCode>>
{
    private readonly IScreeningRepository _repository;
    public GetScreeningCodesQueryHandler(IScreeningRepository repository) => _repository = repository;
    public Task<IReadOnlyList<ScreeningCode>> Handle(GetScreeningCodesQuery request, CancellationToken cancellationToken) =>
        _repository.GetCodesAsync(request.PracticeId, cancellationToken);
}
