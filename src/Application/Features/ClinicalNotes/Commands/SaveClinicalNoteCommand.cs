using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.ClinicalNotes;
using MediatR;

namespace HekCoreApi.Application.Features.ClinicalNotes.Commands;

public sealed record SaveClinicalNoteCommand(int PatientId, int EncounterId, string PracticeId, string Content) : IRequest<ClinicalNote>;

public sealed class SaveClinicalNoteCommandHandler : IRequestHandler<SaveClinicalNoteCommand, ClinicalNote>
{
    private readonly IClinicalNotesRepository _repository;

    public SaveClinicalNoteCommandHandler(IClinicalNotesRepository repository) => _repository = repository;

    public Task<ClinicalNote> Handle(SaveClinicalNoteCommand request, CancellationToken cancellationToken) =>
        _repository.SaveAsync(request.PatientId, request.EncounterId, request.PracticeId, request.Content, cancellationToken);
}
