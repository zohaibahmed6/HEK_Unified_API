using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Acc45;
using MediatR;

namespace HekCoreApi.Application.Features.Acc45.Commands;

public sealed record SaveAcc45FormCommand(Guid SessionKey, string FormInstanceId, HealthLinkSession Session, FormSaveInput Input) : IRequest<FormData>;

public sealed class SaveAcc45FormCommandHandler : IRequestHandler<SaveAcc45FormCommand, FormData>
{
    private readonly IAcc45Repository _repository;
    public SaveAcc45FormCommandHandler(IAcc45Repository repository) => _repository = repository;
    public Task<FormData> Handle(SaveAcc45FormCommand request, CancellationToken cancellationToken) =>
        _repository.SaveFormAsync(request.SessionKey, request.FormInstanceId, request.Session, request.Input, cancellationToken);
}

public sealed record DispatchAcc45ActionCommand(Guid SessionKey, HealthLinkSession Session, FormActionInput Input) : IRequest<FormActionResult>;

public sealed class DispatchAcc45ActionCommandHandler : IRequestHandler<DispatchAcc45ActionCommand, FormActionResult>
{
    private readonly IAcc45Repository _repository;
    public DispatchAcc45ActionCommandHandler(IAcc45Repository repository) => _repository = repository;
    public Task<FormActionResult> Handle(DispatchAcc45ActionCommand request, CancellationToken cancellationToken) =>
        _repository.DispatchActionAsync(request.SessionKey, request.Session, request.Input, cancellationToken);
}
