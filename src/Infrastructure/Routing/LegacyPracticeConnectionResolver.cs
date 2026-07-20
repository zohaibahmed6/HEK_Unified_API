using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Domain.Exceptions;

namespace HekCoreApi.Infrastructure.Routing;

/// <summary>
/// Combines the tenant registry's routing target (ADR-001: which physical server/DB a practice
/// lives on) with credentials resolved via <see cref="ISecretProvider"/> (never hardcoded - direct
/// replacement for the legacy per-practice Web.config connection-string convention this project
/// exists to retire). The credential secret key is scoped per DB server host, not per practice, so
/// practices sharing a physical server (ADR-001) share one credential lookup.
/// </summary>
public sealed class LegacyPracticeConnectionResolver : ILegacyPracticeConnectionResolver
{
    private readonly ITenantRegistryService _tenantRegistry;
    private readonly ISecretProvider _secretProvider;

    public LegacyPracticeConnectionResolver(ITenantRegistryService tenantRegistry, ISecretProvider secretProvider)
    {
        _tenantRegistry = tenantRegistry;
        _secretProvider = secretProvider;
    }

    public async Task<string> ResolveAsync(string practiceId, CancellationToken ct = default)
    {
        var route = await _tenantRegistry.ResolveRouteAsync(practiceId, ct)
            ?? throw new NotFoundException($"Practice '{practiceId}' is not registered.");

        var credentialSecretKey = $"Legacy:DbCredentials:{route.DbServerHost}";
        var credential = await _secretProvider.GetRequiredSecretAsync(credentialSecretKey, ct);

        return $"Server={route.DbServerHost};Database={route.DbName};{credential};TrustServerCertificate=True;";
    }
}
