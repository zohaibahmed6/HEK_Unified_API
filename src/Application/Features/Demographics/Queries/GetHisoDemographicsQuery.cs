using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Demographics;
using MediatR;

namespace HekCoreApi.Application.Features.Demographics.Queries;

public sealed record GetHisoDemographicsQuery(int PatientId, HealthLinkSession Session) : IRequest<DemographicsHiso?>;

public sealed class GetHisoDemographicsQueryHandler : IRequestHandler<GetHisoDemographicsQuery, DemographicsHiso?>
{
    private readonly IDemographicsRepository _repository;

    public GetHisoDemographicsQueryHandler(IDemographicsRepository repository) => _repository = repository;

    public Task<DemographicsHiso?> Handle(GetHisoDemographicsQuery request, CancellationToken cancellationToken) =>
        _repository.GetHisoAsync(request.PatientId, request.Session, cancellationToken);
}
