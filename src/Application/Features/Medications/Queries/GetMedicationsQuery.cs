using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Medications;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Medications.Queries;

public sealed record GetMedicationsQuery(OriginScope Origin, int PatientId, int EncounterId, HealthLinkSession HisoSession, string? View) : IRequest<IReadOnlyList<Medication>>;

public sealed class GetMedicationsQueryHandler : IRequestHandler<GetMedicationsQuery, IReadOnlyList<Medication>>
{
    private readonly IMedicationsRepository _repository;

    public GetMedicationsQueryHandler(IMedicationsRepository repository) => _repository = repository;

    public Task<IReadOnlyList<Medication>> Handle(GetMedicationsQuery request, CancellationToken cancellationToken) =>
        _repository.GetAsync(request.Origin, request.PatientId, request.EncounterId, request.HisoSession, request.View, cancellationToken);
}
