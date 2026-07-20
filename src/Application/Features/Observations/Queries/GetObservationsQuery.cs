using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Observations;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Observations.Queries;

public sealed record GetObservationsQuery(OriginScope Origin, int PatientId, int EncounterId, HealthLinkSession HisoSession, string? ConceptId) : IRequest<IReadOnlyList<Observation>>;

public sealed class GetObservationsQueryHandler : IRequestHandler<GetObservationsQuery, IReadOnlyList<Observation>>
{
    private readonly IObservationsRepository _repository;

    public GetObservationsQueryHandler(IObservationsRepository repository) => _repository = repository;

    public Task<IReadOnlyList<Observation>> Handle(GetObservationsQuery request, CancellationToken cancellationToken) =>
        _repository.GetAsync(request.Origin, request.PatientId, request.EncounterId, request.HisoSession, request.ConceptId, cancellationToken);
}
