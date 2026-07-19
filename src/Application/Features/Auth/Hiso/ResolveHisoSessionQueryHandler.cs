using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Application.Features.Auth.Hiso;

/// <summary>
/// Adds enforced 12-hour expiry and explicit security-event logging on top of HISO's unchanged
/// SessionGUID resolution mechanism (ADR-004) - replacing the current swallowed-exception pattern
/// on failed/invalid session lookups. Never logs the raw SessionGUID or PHI.
/// </summary>
public sealed class ResolveHisoSessionQueryHandler : IRequestHandler<ResolveHisoSessionQuery, HisoSessionLookupResult>
{
    private readonly IHisoSessionRepository _repository;
    private readonly HisoSessionOptions _options;
    private readonly ILogger<ResolveHisoSessionQueryHandler> _logger;

    public ResolveHisoSessionQueryHandler(
        IHisoSessionRepository repository,
        IOptions<HisoSessionOptions> options,
        ILogger<ResolveHisoSessionQueryHandler> logger)
    {
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HisoSessionLookupResult> Handle(ResolveHisoSessionQuery request, CancellationToken cancellationToken)
    {
        var context = await _repository.FindBySessionGuidAsync(request.SessionGuid, request.ServerAddress, cancellationToken);

        if (context is null)
        {
            _logger.LogWarning(
                "HISO session lookup failed: no matching session for server address {ServerAddress}.",
                request.ServerAddress);
            return new HisoSessionLookupResult(HisoSessionLookupStatus.NotFound, null);
        }

        var expiresAt = context.SessionCreatedAtUtc.AddHours(_options.ExpiryHours);
        if (DateTimeOffset.UtcNow > expiresAt)
        {
            _logger.LogWarning(
                "HISO session lookup failed: session for practice {PracticeId} expired at {ExpiresAt}.",
                context.PracticeId,
                expiresAt);
            return new HisoSessionLookupResult(HisoSessionLookupStatus.Expired, null);
        }

        return new HisoSessionLookupResult(HisoSessionLookupStatus.Success, context);
    }
}
