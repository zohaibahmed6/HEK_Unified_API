using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>
/// Real ERMS DMS connection routing: same tenant-registry route (server + credential) as
/// <see cref="ErmsPracticeConnectionResolver"/>, database swapped to the real DMS catalog name
/// (confirmed from legacy Web.config: `ConectionStringPMS_NZ_DMS` shares its server/credentials with
/// `ConectionStringPMS_NZ`, only `Initial Catalog=DMS_PMS` differs) - no separate per-practice DMS
/// secret to configure.
/// </summary>
public sealed class ErmsDmsConnectionResolver : IErmsDmsConnectionResolver
{
    private readonly ITenantRegistryService _tenantRegistry;
    private readonly ISecretProvider _secretProvider;
    private readonly IConfiguration _configuration;

    public ErmsDmsConnectionResolver(ITenantRegistryService tenantRegistry, ISecretProvider secretProvider, IConfiguration configuration)
    {
        _tenantRegistry = tenantRegistry;
        _secretProvider = secretProvider;
        _configuration = configuration;
    }

    public async Task<string> ResolveAsync(RoutingContext routingContext, CancellationToken ct = default)
    {
        var route = await _tenantRegistry.ResolveRouteAsync(routingContext, ct)
            ?? throw new NotFoundException($"ERMS practice '{routingContext.PracticeId}' (code '{routingContext.PracticeCode}', environment '{routingContext.Environment}') is not registered.");

        var credentialSecretKey = $"Erms:DbCredentials:{route.DbServerHost}";
        var credential = await _secretProvider.GetRequiredSecretAsync(credentialSecretKey, ct);
        var dmsDatabaseName = _configuration["Erms:DmsDatabaseName"] ?? "DMS_PMS";

        return $"Server={route.DbServerHost};Database={dmsDatabaseName};{credential};TrustServerCertificate=True;";
    }
}
