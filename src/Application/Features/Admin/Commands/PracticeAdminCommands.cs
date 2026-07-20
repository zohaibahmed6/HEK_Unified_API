using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Admin;
using MediatR;

namespace HekCoreApi.Application.Features.Admin.Commands;

public sealed record RegisterPracticeCommand(PracticeInput Input) : IRequest<Practice>;

public sealed class RegisterPracticeCommandHandler : IRequestHandler<RegisterPracticeCommand, Practice>
{
    private readonly IPracticeAdminRepository _repository;
    public RegisterPracticeCommandHandler(IPracticeAdminRepository repository) => _repository = repository;
    public Task<Practice> Handle(RegisterPracticeCommand request, CancellationToken cancellationToken) => _repository.RegisterAsync(request.Input, cancellationToken);
}

public sealed record UpdatePracticeCommand(string PracticeId, PracticeInput Input) : IRequest<Practice?>;

public sealed class UpdatePracticeCommandHandler : IRequestHandler<UpdatePracticeCommand, Practice?>
{
    private readonly IPracticeAdminRepository _repository;
    public UpdatePracticeCommandHandler(IPracticeAdminRepository repository) => _repository = repository;
    public Task<Practice?> Handle(UpdatePracticeCommand request, CancellationToken cancellationToken) => _repository.UpdateAsync(request.PracticeId, request.Input, cancellationToken);
}
