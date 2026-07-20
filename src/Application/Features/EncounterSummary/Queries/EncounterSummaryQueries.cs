using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.EncounterSummary;
using MediatR;

namespace HekCoreApi.Application.Features.EncounterSummary.Queries;

public sealed record GetTemplateSchemaQuery(string PracticeId, string Identifier) : IRequest<TemplateSchema?>;

public sealed class GetTemplateSchemaQueryHandler : IRequestHandler<GetTemplateSchemaQuery, TemplateSchema?>
{
    private readonly IEncounterSummaryRepository _repository;
    public GetTemplateSchemaQueryHandler(IEncounterSummaryRepository repository) => _repository = repository;
    public Task<TemplateSchema?> Handle(GetTemplateSchemaQuery request, CancellationToken cancellationToken) =>
        _repository.GetTemplateSchemaAsync(request.PracticeId, request.Identifier, cancellationToken);
}

public sealed record GetEncounterSummaryQuery(int PatientId, int EncounterId, string PracticeId, string Identifier) : IRequest<EncounterSummaryData?>;

public sealed class GetEncounterSummaryQueryHandler : IRequestHandler<GetEncounterSummaryQuery, EncounterSummaryData?>
{
    private readonly IEncounterSummaryRepository _repository;
    public GetEncounterSummaryQueryHandler(IEncounterSummaryRepository repository) => _repository = repository;
    public Task<EncounterSummaryData?> Handle(GetEncounterSummaryQuery request, CancellationToken cancellationToken) =>
        _repository.GetSummaryAsync(request.PatientId, request.EncounterId, request.PracticeId, request.Identifier, cancellationToken);
}
