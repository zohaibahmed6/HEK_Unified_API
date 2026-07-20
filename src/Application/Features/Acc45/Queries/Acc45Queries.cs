using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Features.Auth.Hiso;
using HekCoreApi.Contracts.Acc45;
using HekCoreApi.Domain.Exceptions;
using MediatR;

namespace HekCoreApi.Application.Features.Acc45.Queries;

public sealed record GetAcc45VersionQuery : IRequest<VersionInfo>;

public sealed class GetAcc45VersionQueryHandler : IRequestHandler<GetAcc45VersionQuery, VersionInfo>
{
    private readonly IAcc45Repository _repository;
    public GetAcc45VersionQueryHandler(IAcc45Repository repository) => _repository = repository;
    public Task<VersionInfo> Handle(GetAcc45VersionQuery request, CancellationToken cancellationToken) => _repository.GetVersionAsync(cancellationToken);
}

/// <summary>
/// Resolves sessionKey -> full context (HISO-BR-01/02), reusing Block 1's session-lookup query.
/// FLAGGED SIMPLIFICATION: passes the caller's own token practiceId as the "server address" lookup
/// key (ADR-007 routing) since a REST caller has no concept of "which HISO address was called" -
/// that context only exists in the not-yet-built SOAP edge adapter. See PROJECT_STATUS.md item 25.
/// </summary>
public sealed record GetAcc45SessionQuery(Guid SessionKey, string CallerPracticeId) : IRequest<SessionContext>;

public sealed class GetAcc45SessionQueryHandler : IRequestHandler<GetAcc45SessionQuery, SessionContext>
{
    private readonly IMediator _mediator;
    public GetAcc45SessionQueryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<SessionContext> Handle(GetAcc45SessionQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CallerPracticeId), cancellationToken);
        if (result.Status != HisoSessionLookupStatus.Success || result.Context is null)
        {
            throw new UnauthorizedException("Invalid session key.");
        }

        return new SessionContext(result.Context.ProviderId, int.TryParse(result.Context.PatientId, out var pid) ? pid : 0, result.Context.AppointmentId, result.Context.PracticeId, null, null, null);
    }
}

public sealed record GetAcc45DeliveryOptionsQuery(HealthLinkSession Session) : IRequest<DeliveryOptions>;

public sealed class GetAcc45DeliveryOptionsQueryHandler : IRequestHandler<GetAcc45DeliveryOptionsQuery, DeliveryOptions>
{
    private readonly IAcc45Repository _repository;
    public GetAcc45DeliveryOptionsQueryHandler(IAcc45Repository repository) => _repository = repository;
    public Task<DeliveryOptions> Handle(GetAcc45DeliveryOptionsQuery request, CancellationToken cancellationToken) => _repository.GetDeliveryOptionsAsync(request.Session, cancellationToken);
}

public sealed record GetAcc45FormsQuery(Guid SessionKey, HealthLinkSession Session, string Mode) : IRequest<FormData>;

public sealed class GetAcc45FormsQueryHandler : IRequestHandler<GetAcc45FormsQuery, FormData>
{
    private readonly IAcc45Repository _repository;
    public GetAcc45FormsQueryHandler(IAcc45Repository repository) => _repository = repository;
    public Task<FormData> Handle(GetAcc45FormsQuery request, CancellationToken cancellationToken) => _repository.GetFormsAsync(request.SessionKey, request.Session, request.Mode, cancellationToken);
}

public sealed record GetAcc45FormQuery(Guid SessionKey, string FormInstanceId, HealthLinkSession Session) : IRequest<FormData?>;

public sealed class GetAcc45FormQueryHandler : IRequestHandler<GetAcc45FormQuery, FormData?>
{
    private readonly IAcc45Repository _repository;
    public GetAcc45FormQueryHandler(IAcc45Repository repository) => _repository = repository;
    public Task<FormData?> Handle(GetAcc45FormQuery request, CancellationToken cancellationToken) => _repository.GetFormAsync(request.SessionKey, request.FormInstanceId, request.Session, cancellationToken);
}
