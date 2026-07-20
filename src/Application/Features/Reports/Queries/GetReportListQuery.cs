using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Reports;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Reports.Queries;

public sealed record GetReportListQuery(
    ReportKind Kind, OriginScope Origin, int PatientId, int EncounterId, HealthLinkSession HisoSession,
    DateOnly? SinceDate, DateOnly? UntilDate, string? SortOrder) : IRequest<IReadOnlyList<ReportSummary>>;

public sealed class GetReportListQueryHandler : IRequestHandler<GetReportListQuery, IReadOnlyList<ReportSummary>>
{
    private readonly IReportsRepository _repository;

    public GetReportListQueryHandler(IReportsRepository repository) => _repository = repository;

    public Task<IReadOnlyList<ReportSummary>> Handle(GetReportListQuery request, CancellationToken cancellationToken) =>
        _repository.GetListAsync(request.Kind, request.Origin, request.PatientId, request.EncounterId, request.HisoSession, request.SinceDate, request.UntilDate, request.SortOrder, cancellationToken);
}
