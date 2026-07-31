using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <inheritdoc cref="IHisoDmsConnectionResolver"/>
public sealed class HisoDmsConnectionResolver : IHisoDmsConnectionResolver
{
    private readonly IHisoSessionRegistryRepository _sessionRegistry;
    private readonly ISecretProvider _secretProvider;
    private readonly IConfiguration _configuration;

    public HisoDmsConnectionResolver(IHisoSessionRegistryRepository sessionRegistry, ISecretProvider secretProvider, IConfiguration configuration)
    {
        _sessionRegistry = sessionRegistry;
        _secretProvider = secretProvider;
        _configuration = configuration;
    }

    public async Task<string> ResolveAsync(string practiceId, CancellationToken ct = default)
    {
        var route = await _sessionRegistry.FindByPracticeIdAsync(practiceId, ct)
            ?? throw new NotFoundException($"Practice '{practiceId}' has no active HisoSessions entry.");

        var credentialSecretKey = $"Legacy:DbCredentials:{route.DbServerHost}";
        var credential = await _secretProvider.GetRequiredSecretAsync(credentialSecretKey, ct);
        var dmsDatabaseName = _configuration["Hiso:DmsDatabaseName"] ?? "DMS_PMS";

        return $"Server={route.DbServerHost};Database={dmsDatabaseName};{credential};TrustServerCertificate=True;";
    }
}
