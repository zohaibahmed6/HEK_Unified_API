using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Default <see cref="IAwsDocumentService"/> registration until Zohaib maps the real `AWSDoc.dll`
/// (PROJECT_STATUS.md, HISO wire-compat rebuild). Always reports AWS disabled, matching legacy's own
/// behavior when a practice has no AWS document storage configured - callers fall through to the
/// plain, non-AWS procedure path, never a hard failure.
/// </summary>
public sealed class NotConfiguredAwsDocumentService : IAwsDocumentService
{
    public Task<bool> CheckAwsIsEnabledAsync(int practiceId, CancellationToken ct = default) => Task.FromResult(false);

    public Task<string?> DocumentGetByDocumentKeyJsonResultAsync(string documentKey, int practiceId, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    public Task<string?> GetDocumentStatusFromIndiciAsync(string documentKey, int practiceId, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
}
