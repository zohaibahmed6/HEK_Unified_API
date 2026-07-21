using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Domain.Exceptions;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>
/// Real KARO/HSS connection routing: `"ConnIndiciDB" + practiceSuffix` naming, sourced via
/// <see cref="ISecretProvider"/> under `Karo:DbCredentials:ConnIndiciDB{practiceSuffix}` (never
/// hardcoded - direct replacement for the legacy Web.config connection-string convention this
/// project exists to retire).
/// </summary>
public sealed class KaroPracticeConnectionResolver : IKaroPracticeConnectionResolver
{
    private readonly ISecretProvider _secretProvider;

    public KaroPracticeConnectionResolver(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public async Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default)
    {
        var key = $"Karo:DbCredentials:ConnIndiciDB{practiceSuffix}";
        var connectionString = await _secretProvider.GetSecretAsync(key, ct);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new NotFoundException($"KARO/HSS connection target 'ConnIndiciDB{practiceSuffix}' is not configured ({key}).")
            : connectionString;
    }
}
