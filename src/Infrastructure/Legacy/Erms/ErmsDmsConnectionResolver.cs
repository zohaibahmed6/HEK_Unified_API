using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Domain.Exceptions;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>Real ERMS DMS connection routing: `"ConnDMSDB" + practiceSuffix`, sourced via <see cref="ISecretProvider"/>.</summary>
public sealed class ErmsDmsConnectionResolver : IErmsDmsConnectionResolver
{
    private readonly ISecretProvider _secretProvider;

    public ErmsDmsConnectionResolver(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public async Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default)
    {
        var key = $"Erms:DbCredentials:ConnDMSDB{practiceSuffix}";
        var connectionString = await _secretProvider.GetSecretAsync(key, ct);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new NotFoundException($"ERMS DMS connection target 'ConnDMSDB{practiceSuffix}' is not configured ({key}).")
            : connectionString;
    }
}
