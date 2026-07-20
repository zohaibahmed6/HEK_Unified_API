using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Demographics;
using MediatR;

namespace HekCoreApi.Application.Features.Demographics.Queries;

public sealed record GetColDemographicsQuery(int PatientId, string PracticeId) : IRequest<DemographicsCol?>;

public sealed class GetColDemographicsQueryHandler : IRequestHandler<GetColDemographicsQuery, DemographicsCol?>
{
    private readonly IDemographicsRepository _repository;

    public GetColDemographicsQueryHandler(IDemographicsRepository repository) => _repository = repository;

    public Task<DemographicsCol?> Handle(GetColDemographicsQuery request, CancellationToken cancellationToken) =>
        _repository.GetColAsync(request.PatientId, request.PracticeId, cancellationToken);
}
