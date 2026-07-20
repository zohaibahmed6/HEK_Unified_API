using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Recalls;
using MediatR;

namespace HekCoreApi.Application.Features.Recalls.Commands;

public sealed record SaveRecallCommand(int PatientId, string PracticeId, RecallInput Input) : IRequest<Recall>;

public sealed class SaveRecallCommandHandler : IRequestHandler<SaveRecallCommand, Recall>
{
    private readonly IRecallsRepository _repository;
    public SaveRecallCommandHandler(IRecallsRepository repository) => _repository = repository;
    public Task<Recall> Handle(SaveRecallCommand request, CancellationToken cancellationToken) =>
        _repository.SaveAsync(request.PatientId, request.PracticeId, request.Input, cancellationToken);
}
