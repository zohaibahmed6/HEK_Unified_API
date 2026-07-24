using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Fallback <see cref="IAwsDocumentService"/> - no longer the default registration (see
/// <see cref="AwsDocumentService"/>, wired 2026-07-24), kept for environments/tests that want AWS
/// forced off without touching config. Always reports AWS disabled, matching legacy's own behavior
/// when a practice has no AWS document storage configured - callers fall through to the plain,
/// non-AWS procedure path, never a hard failure.
/// </summary>
public sealed class NotConfiguredAwsDocumentService : IAwsDocumentService
{
    public Task<bool> CheckAwsIsEnabledAsync(int practiceId, string connectionString, CancellationToken ct = default) => Task.FromResult(false);

    public Task<string?> DocumentGetByDocumentKeyJsonResultAsync(string documentKey, int practiceId, string dmsConnectionString, string pmsConnectionString, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    public Task<AwsDocumentStatus?> GetDocumentStatusFromIndiciAsync(string documentKey, int practiceId, string connectionString, CancellationToken ct = default) =>
        Task.FromResult<AwsDocumentStatus?>(null);

    public Task<byte[]?> DownloadFromAwsAsync(AwsDocumentStatus status, CancellationToken ct = default) => Task.FromResult<byte[]?>(null);

    public void ConfigureAws(string baseUrl, string secretKey, int jwtTokenExpiryMinutes, int s3RequestTimeoutSeconds)
    {
    }
}
