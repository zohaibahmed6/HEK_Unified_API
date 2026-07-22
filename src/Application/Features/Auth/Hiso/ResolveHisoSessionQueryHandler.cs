using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Application.Features.Auth.Hiso;

/// <summary>
/// Resolves a HISO SessionGUID via the central session registry (HekTenantRegistry.HisoSessions,
/// 2026-07-22) rather than the earlier Host-header/<c>Hiso:ConnectionStrings:{serverAddress}</c>
/// mechanism: look up the SessionGuid's own row (mirrors Practices' shape - carries its own
/// DbServerHost/DbName directly), build the connection string from it, then fetch the real session
/// row from that practice's own <c>tblHealthLinkSession</c>. Deliberately does **not** go through
/// <c>ILegacyPracticeConnectionResolver</c>/<c>Practices</c> - that path is reserved for ERMS/KARO
/// routing only, per explicit instruction. Also adds the enforced 12-hour expiry and explicit
/// security-event logging on top of HISO's unchanged SessionGUID mechanism (ADR-004) - replacing the
/// legacy swallowed-exception pattern on failed/invalid lookups. Never logs the raw SessionGUID or PHI.
/// </summary>
public sealed class ResolveHisoSessionQueryHandler : IRequestHandler<ResolveHisoSessionQuery, HisoSessionLookupResult>
{
    private readonly IHisoSessionRegistryRepository _sessionRegistry;
    private readonly ISecretProvider _secretProvider;
    private readonly IHisoSessionRepository _repository;
    private readonly HisoSessionOptions _options;
    private readonly ILogger<ResolveHisoSessionQueryHandler> _logger;

    public ResolveHisoSessionQueryHandler(
        IHisoSessionRegistryRepository sessionRegistry,
        ISecretProvider secretProvider,
        IHisoSessionRepository repository,
        IOptions<HisoSessionOptions> options,
        ILogger<ResolveHisoSessionQueryHandler> logger)
    {
        _sessionRegistry = sessionRegistry;
        _secretProvider = secretProvider;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HisoSessionLookupResult> Handle(ResolveHisoSessionQuery request, CancellationToken cancellationToken)
    {
        var route = await _sessionRegistry.FindAsync(request.SessionGuid, cancellationToken);
        if (route is null)
        {
            _logger.LogWarning("HISO session lookup failed: SessionGuid not found in the central session registry.");
            return new HisoSessionLookupResult(HisoSessionLookupStatus.NotFound, null);
        }

        var expiresAt = route.CreatedAt.AddHours(_options.ExpiryHours);
        if (DateTimeOffset.Now > expiresAt)
        {
            _logger.LogWarning(
                "HISO session lookup failed: session for practice {PracticeId} expired at {ExpiresAt}.",
                route.PracticeId,
                expiresAt);
            return new HisoSessionLookupResult(HisoSessionLookupStatus.Expired, null);
        }

        // Connection built directly from the row's own DbServerHost/DbName - never from a
        // Hiso:ConnectionStrings:* (or any other connection-string-keyed) secret. The credential
        // secret is keyed by server host for the password only, same pattern
        // LegacyPracticeConnectionResolver already uses for ERMS/KARO/generic routing.
        var credential = await _secretProvider.GetRequiredSecretAsync($"Legacy:DbCredentials:{route.DbServerHost}", cancellationToken);
        var connectionString = $"Server={route.DbServerHost};Database={route.DbName};{credential};TrustServerCertificate=True;";

        var context = await _repository.FindBySessionGuidAsync(request.SessionGuid, connectionString, cancellationToken);
        if (context is null)
        {
            _logger.LogWarning(
                "HISO session lookup failed: SessionGuid was registered centrally for practice {PracticeId} but not found in that practice's own session table.",
                route.PracticeId);
            return new HisoSessionLookupResult(HisoSessionLookupStatus.NotFound, null);
        }

        return new HisoSessionLookupResult(HisoSessionLookupStatus.Success, context);
    }
}
