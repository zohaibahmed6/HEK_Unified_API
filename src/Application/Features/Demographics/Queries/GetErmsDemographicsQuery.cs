using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Demographics;
using MediatR;

namespace HekCoreApi.Application.Features.Demographics.Queries;

public sealed record GetErmsDemographicsQuery(int PatientId, string PracticeId) : IRequest<DemographicsErms?>;

public sealed class GetErmsDemographicsQueryHandler : IRequestHandler<GetErmsDemographicsQuery, DemographicsErms?>
{
    private readonly IDemographicsRepository _repository;

    public GetErmsDemographicsQueryHandler(IDemographicsRepository repository) => _repository = repository;

    public Task<DemographicsErms?> Handle(GetErmsDemographicsQuery request, CancellationToken cancellationToken) =>
        _repository.GetErmsAsync(request.PatientId, request.PracticeId, cancellationToken);
}
