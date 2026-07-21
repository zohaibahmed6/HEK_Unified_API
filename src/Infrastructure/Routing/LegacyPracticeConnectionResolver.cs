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

    public async Task<string> ResolveSecondNodeAsync(CancellationToken ct = default)
    {
        var connectionString = await _secretProvider.GetSecretAsync("Hiso:SecondNodeConnectionString", ct);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new NotFoundException("HISO second database node is not configured yet (Hiso:SecondNodeConnectionString).")
            : connectionString;
    }

    public async Task<string> ResolveIndiciMasterAsync(CancellationToken ct = default)
    {
        var connectionString = await _secretProvider.GetSecretAsync("Hiso:IndiciMasterConnectionString", ct);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new NotFoundException("HISO Indici Master database is not configured yet (Hiso:IndiciMasterConnectionString).")
            : connectionString;
    }
}
