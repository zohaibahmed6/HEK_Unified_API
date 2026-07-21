using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Domain.Exceptions;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>Real KARO/HSS DMS connection routing: `"ConnDMSDB" + practiceSuffix`, sourced via <see cref="ISecretProvider"/>.</summary>
public sealed class KaroDmsConnectionResolver : IKaroDmsConnectionResolver
{
    private readonly ISecretProvider _secretProvider;

    public KaroDmsConnectionResolver(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public async Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default)
    {
        var key = $"Karo:DbCredentials:ConnDMSDB{practiceSuffix}";
        var connectionString = await _secretProvider.GetSecretAsync(key, ct);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new NotFoundException($"KARO/HSS DMS connection target 'ConnDMSDB{practiceSuffix}' is not configured ({key}).")
            : connectionString;
    }
}
