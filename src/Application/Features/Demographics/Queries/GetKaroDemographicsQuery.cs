using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Demographics;
using MediatR;

namespace HekCoreApi.Application.Features.Demographics.Queries;

public sealed record GetKaroDemographicsQuery(int PatientId, string PracticeId) : IRequest<DemographicsKaro?>;

public sealed class GetKaroDemographicsQueryHandler : IRequestHandler<GetKaroDemographicsQuery, DemographicsKaro?>
{
    private readonly IDemographicsRepository _repository;

    public GetKaroDemographicsQueryHandler(IDemographicsRepository repository) => _repository = repository;

    public Task<DemographicsKaro?> Handle(GetKaroDemographicsQuery request, CancellationToken cancellationToken) =>
        _repository.GetKaroAsync(request.PatientId, request.PracticeId, cancellationToken);
}
