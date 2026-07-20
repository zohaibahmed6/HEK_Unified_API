using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Screening;
using MediatR;

namespace HekCoreApi.Application.Features.Screening.Commands;

public sealed record SaveScreeningCodeCommand(int PatientId, int EncounterId, string PracticeId, ScreeningCodeInput Input) : IRequest<ScreeningCodeResult>;

public sealed class SaveScreeningCodeCommandHandler : IRequestHandler<SaveScreeningCodeCommand, ScreeningCodeResult>
{
    private readonly IScreeningRepository _repository;
    public SaveScreeningCodeCommandHandler(IScreeningRepository repository) => _repository = repository;
    public Task<ScreeningCodeResult> Handle(SaveScreeningCodeCommand request, CancellationToken cancellationToken) =>
        _repository.SaveAsync(request.PatientId, request.EncounterId, request.PracticeId, request.Input, cancellationToken);
}
