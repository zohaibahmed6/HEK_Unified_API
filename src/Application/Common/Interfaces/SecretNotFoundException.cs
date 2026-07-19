namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Thrown by <see cref="ISecretProvider.GetRequiredSecretAsync"/> when a required secret is missing.</summary>
public sealed class SecretNotFoundException : Exception
{
    public SecretNotFoundException(string key)
        : base($"Required secret '{key}' was not found. Configure it via environment variable or the active secret store.")
    {
    }
}
