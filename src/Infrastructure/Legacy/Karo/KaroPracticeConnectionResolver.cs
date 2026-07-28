using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Domain.Exceptions;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>
/// Combines the tenant registry's routing target (ADR-001) with credentials resolved via
/// <see cref="ISecretProvider"/> - replaces the old flat `Karo:DbCredentials:ConnIndiciDB{practiceSuffix}`
/// secret convention. The credential secret key is scoped per DB server host (not per practice), so
/// practices sharing a physical server share one credential lookup, same pattern as
/// <see cref="ILegacyPracticeConnectionResolver"/>.
/// </summary>
public sealed class KaroPracticeConnectionResolver : IKaroPracticeConnectionResolver
{
    private readonly ITenantRegistryService _tenantRegistry;
    private readonly ISecretProvider _secretProvider;

    public KaroPracticeConnectionResolver(ITenantRegistryService tenantRegistry, ISecretProvider secretProvider)
    {
        _tenantRegistry = tenantRegistry;
        _secretProvider = secretProvider;
    }

    public async Task<string> ResolveAsync(RoutingContext context, CancellationToken ct = default)
    {
        var route = await _tenantRegistry.ResolveRouteAsync(context, ct)
            ?? throw new NotFoundException($"KARO/HSS practice '{context.PracticeId}' (code '{context.PracticeCode}', environment '{context.Environment}') is not registered.");

        var credentialSecretKey = $"Karo:DbCredentials:{route.DbServerHost}";
        var credential = await _secretProvider.GetRequiredSecretAsync(credentialSecretKey, ct);

        return $"Server={route.DbServerHost};Database={route.DbName};{credential};TrustServerCertificate=True;";
    }
}
