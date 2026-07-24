using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Domain.Exceptions;
using HekCoreApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
    private readonly TenantRegistryDbContext _dbContext;

    public LegacyPracticeConnectionResolver(ITenantRegistryService tenantRegistry, ISecretProvider secretProvider, TenantRegistryDbContext dbContext)
    {
        _tenantRegistry = tenantRegistry;
        _secretProvider = secretProvider;
        _dbContext = dbContext;
    }

    public Task<string> ResolveAsync(string practiceId, CancellationToken ct = default) =>
        ResolveAsync(RoutingContext.FromPracticeId(practiceId, default), ct);

    public async Task<string> ResolveAsync(RoutingContext context, CancellationToken ct = default)
    {
        var route = await _tenantRegistry.ResolveRouteAsync(context, ct)
            ?? throw new NotFoundException($"Practice '{context.PracticeId}' (code '{context.PracticeCode}', environment '{context.Environment}') is not registered.");

        var credentialSecretKey = $"Legacy:DbCredentials:{route.DbServerHost}";
        var credential = await _secretProvider.GetRequiredSecretAsync(credentialSecretKey, ct);

        return $"Server={route.DbServerHost};Database={route.DbName};{credential};TrustServerCertificate=True;";
    }

    public Task<string> ResolveSecondNodeAsync(CancellationToken ct = default) =>
        ResolveGlobalConnectionAsync("Hiso:SecondNode", ct);

    public Task<string> ResolveIndiciMasterAsync(CancellationToken ct = default) =>
        ResolveGlobalConnectionAsync("Hiso:IndiciMaster", ct);

    /// <summary>
    /// v1.1 spec follow-through, Step 7 (2026-07-24): reads from the real <see cref="Domain.Entities.LegacyGlobalConnectionEntry"/>
    /// registry table instead of a single global env-var secret - confirmed from real legacy source
    /// that both HISO's second-node and Indici-master connections are genuinely global-per-environment
    /// (not per-practice), so this is one row per environment-scoped <paramref name="key"/>, swappable
    /// without a redeploy. Falls back to the old env-var secret if no registry row exists yet, so an
    /// environment that hasn't been migrated to a registry row doesn't silently break.
    /// </summary>
    private async Task<string> ResolveGlobalConnectionAsync(string key, CancellationToken ct)
    {
        var entry = await _dbContext.LegacyGlobalConnections
            .FirstOrDefaultAsync(c => c.Key == key && c.IsActive, ct);

        if (entry is not null)
        {
            var credential = await _secretProvider.GetRequiredSecretAsync(entry.CredentialSecretKey, ct);
            return $"Server={entry.DbServerHost};Database={entry.DbName};{credential};TrustServerCertificate=True;";
        }

        var legacySecretKey = key switch
        {
            "Hiso:SecondNode" => "Hiso:SecondNodeConnectionString",
            "Hiso:IndiciMaster" => "Hiso:IndiciMasterConnectionString",
            _ => throw new NotFoundException($"No registry row or legacy secret mapping for global connection key '{key}'."),
        };

        var connectionString = await _secretProvider.GetSecretAsync(legacySecretKey, ct);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new NotFoundException($"Global connection '{key}' is not configured (no LegacyGlobalConnections registry row and no {legacySecretKey} secret).")
            : connectionString;
    }
}
