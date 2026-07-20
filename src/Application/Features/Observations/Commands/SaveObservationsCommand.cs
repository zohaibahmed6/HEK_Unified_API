using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Observations;
using MediatR;

namespace HekCoreApi.Application.Features.Observations.Commands;

public sealed record SaveObservationsCommand(int PatientId, int EncounterId, string PracticeId, ObservationInput Input) : IRequest<Observation>;

public sealed class SaveObservationsCommandHandler : IRequestHandler<SaveObservationsCommand, Observation>
{
    private readonly IObservationsRepository _repository;

    public SaveObservationsCommandHandler(IObservationsRepository repository) => _repository = repository;

    public Task<Observation> Handle(SaveObservationsCommand request, CancellationToken cancellationToken) =>
        _repository.SaveAsync(request.PatientId, request.EncounterId, request.PracticeId, request.Input, cancellationToken);
}
