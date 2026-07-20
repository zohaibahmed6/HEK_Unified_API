using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Reports;
using MediatR;

namespace HekCoreApi.Application.Features.Reports.Queries;

public sealed record GetReportDetailQuery(ReportKind Kind, string PracticeId, string ReportId) : IRequest<ReportContent?>;

public sealed class GetReportDetailQueryHandler : IRequestHandler<GetReportDetailQuery, ReportContent?>
{
    private readonly IReportsRepository _repository;

    public GetReportDetailQueryHandler(IReportsRepository repository) => _repository = repository;

    public Task<ReportContent?> Handle(GetReportDetailQuery request, CancellationToken cancellationToken) =>
        _repository.GetDetailAsync(request.Kind, request.PracticeId, request.ReportId, cancellationToken);
}
