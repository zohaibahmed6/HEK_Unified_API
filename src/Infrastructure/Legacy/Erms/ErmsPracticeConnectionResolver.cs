using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Domain.Exceptions;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

public sealed class ErmsPracticeConnectionResolver : IErmsPracticeConnectionResolver
{
    private readonly ISecretProvider _secretProvider;

    public ErmsPracticeConnectionResolver(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public async Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default)
    {
        var key = $"Erms:DbCredentials:ConnIndiciDB{practiceSuffix}";
        var connectionString = await _secretProvider.GetSecretAsync(key, ct);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new NotFoundException($"ERMS connection target 'ConnIndiciDB{practiceSuffix}' is not configured ({key}).")
            : connectionString;
    }
}
