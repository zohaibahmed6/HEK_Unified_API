using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.EncounterSummary;
using MediatR;

namespace HekCoreApi.Application.Features.EncounterSummary.Commands;

public sealed record SaveEncounterSummaryCommand(int PatientId, int EncounterId, string PracticeId, EncounterSummaryInput Input) : IRequest<EncounterSummaryData>;

public sealed class SaveEncounterSummaryCommandHandler : IRequestHandler<SaveEncounterSummaryCommand, EncounterSummaryData>
{
    private readonly IEncounterSummaryRepository _repository;
    public SaveEncounterSummaryCommandHandler(IEncounterSummaryRepository repository) => _repository = repository;
    public Task<EncounterSummaryData> Handle(SaveEncounterSummaryCommand request, CancellationToken cancellationToken) =>
        _repository.SaveSummaryAsync(request.PatientId, request.EncounterId, request.PracticeId, request.Input, cancellationToken);
}
