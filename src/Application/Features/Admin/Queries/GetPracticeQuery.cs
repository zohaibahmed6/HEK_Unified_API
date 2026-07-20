using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Admin;
using MediatR;

namespace HekCoreApi.Application.Features.Admin.Queries;

public sealed record GetPracticeQuery(string PracticeId) : IRequest<Practice?>;

public sealed class GetPracticeQueryHandler : IRequestHandler<GetPracticeQuery, Practice?>
{
    private readonly IPracticeAdminRepository _repository;
    public GetPracticeQueryHandler(IPracticeAdminRepository repository) => _repository = repository;
    public Task<Practice?> Handle(GetPracticeQuery request, CancellationToken cancellationToken) => _repository.GetAsync(request.PracticeId, cancellationToken);
}
