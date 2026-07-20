using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Conditions;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Conditions.Queries;

public sealed record GetConditionsQuery(OriginScope Origin, int PatientId, int EncounterId, HealthLinkSession HisoSession) : IRequest<IReadOnlyList<Condition>>;

public sealed class GetConditionsQueryHandler : IRequestHandler<GetConditionsQuery, IReadOnlyList<Condition>>
{
    private readonly IConditionsRepository _repository;

    public GetConditionsQueryHandler(IConditionsRepository repository) => _repository = repository;

    public Task<IReadOnlyList<Condition>> Handle(GetConditionsQuery request, CancellationToken cancellationToken) =>
        _repository.GetAsync(request.Origin, request.PatientId, request.EncounterId, request.HisoSession, cancellationToken);
}
