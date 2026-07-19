using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HekCoreApi.Infrastructure.Secrets;

/// <summary>
/// Block 0/1 implementation of <see cref="ISecretProvider"/>: resolves secrets from configuration
/// (environment variables, .env via docker-compose, or appsettings for non-sensitive local dev
/// defaults). Swappable later for an Azure Key Vault / HashiCorp Vault-backed implementation with
/// zero caller-side change - callers only ever depend on ISecretProvider.
/// </summary>
public sealed class EnvironmentVariableSecretProvider : ISecretProvider
{
    private readonly IConfiguration _configuration;

    public EnvironmentVariableSecretProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(_configuration[key]);
    }

    public async Task<string> GetRequiredSecretAsync(string key, CancellationToken ct = default)
    {
        var value = await GetSecretAsync(key, ct);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SecretNotFoundException(key);
        }

        return value;
    }
}
