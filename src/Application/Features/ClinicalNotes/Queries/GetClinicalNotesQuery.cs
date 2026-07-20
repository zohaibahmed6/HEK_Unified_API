using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.ClinicalNotes;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.ClinicalNotes.Queries;

public sealed record GetClinicalNotesQuery(
    OriginScope Origin, int PatientId, int EncounterId, HealthLinkSession HisoSession,
    DateOnly? SinceDate, DateOnly? UntilDate, string? SortOrder) : IRequest<IReadOnlyList<ClinicalNote>>;

public sealed class GetClinicalNotesQueryHandler : IRequestHandler<GetClinicalNotesQuery, IReadOnlyList<ClinicalNote>>
{
    private readonly IClinicalNotesRepository _repository;

    public GetClinicalNotesQueryHandler(IClinicalNotesRepository repository) => _repository = repository;

    public Task<IReadOnlyList<ClinicalNote>> Handle(GetClinicalNotesQuery request, CancellationToken cancellationToken) =>
        _repository.GetAsync(request.Origin, request.PatientId, request.EncounterId, request.HisoSession, request.SinceDate, request.UntilDate, request.SortOrder, cancellationToken);
}
