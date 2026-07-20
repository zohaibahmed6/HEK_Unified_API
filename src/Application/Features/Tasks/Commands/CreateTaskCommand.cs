using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Tasks;
using MediatR;

namespace HekCoreApi.Application.Features.Tasks.Commands;

public sealed record CreateTaskCommand(int PatientId, string PracticeId, TaskInput Input) : IRequest<TaskResult>;

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskResult>
{
    private readonly ITasksRepository _repository;
    public CreateTaskCommandHandler(ITasksRepository repository) => _repository = repository;
    public Task<TaskResult> Handle(CreateTaskCommand request, CancellationToken cancellationToken) =>
        _repository.CreateAsync(request.PatientId, request.PracticeId, request.Input, cancellationToken);
}
