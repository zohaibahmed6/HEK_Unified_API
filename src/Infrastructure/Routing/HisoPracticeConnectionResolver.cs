using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Domain.Exceptions;

namespace HekCoreApi.Infrastructure.Routing;

/// <inheritdoc cref="IHisoPracticeConnectionResolver"/>
public sealed class HisoPracticeConnectionResolver : IHisoPracticeConnectionResolver
{
    private readonly IHisoSessionRegistryRepository _sessionRegistry;
    private readonly ISecretProvider _secretProvider;

    public HisoPracticeConnectionResolver(IHisoSessionRegistryRepository sessionRegistry, ISecretProvider secretProvider)
    {
        _sessionRegistry = sessionRegistry;
        _secretProvider = secretProvider;
    }

    public async Task<string> ResolveAsync(string practiceId, CancellationToken ct = default)
    {
        var route = await _sessionRegistry.FindByPracticeIdAsync(practiceId, ct)
            ?? throw new NotFoundException($"Practice '{practiceId}' has no active HisoSessions entry.");

        var credentialSecretKey = $"Legacy:DbCredentials:{route.DbServerHost}";
        var credential = await _secretProvider.GetRequiredSecretAsync(credentialSecretKey, ct);

        return $"Server={route.DbServerHost};Database={route.DbName};{credential};TrustServerCertificate=True;";
    }
}
