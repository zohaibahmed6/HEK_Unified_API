using AWSDocCore;
using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// v1.1 spec follow-through, Step 5: real implementation against the real `AWSDocCore.dll` Zohaib
/// supplied. Confirmed by reflecting the DLL directly (not guessed) - all three real methods
/// (<see cref="IndiciDMS.CheckAWSIsEnabled"/>, <see cref="IndiciDMS.DocumentGetByDocumentKeyJsonResult"/>,
/// <see cref="IndiciDMS.GetDocumentStatusFromIndici"/>) are synchronous and take a connection string
/// directly - wrapped here in <see cref="Task"/> to match this project's async interface convention,
/// not because the underlying call is genuinely asynchronous.
/// </summary>
public sealed class AwsDocumentService : IAwsDocumentService
{
    public void ConfigureAws(string baseUrl, string secretKey, int jwtTokenExpiryMinutes, int s3RequestTimeoutSeconds) =>
        DocumentManager.SetBaseUrlAndSecretKey(baseUrl, secretKey, jwtTokenExpiryMinutes, s3RequestTimeoutSeconds);

    /// <summary>
    /// v1.1 spec follow-through (2026-07-24): real flow, confirmed against `AWSDocCore`'s own source
    /// (`DocumentManager.Download(DocumentFileStatus)`/`IndiciDMS.cs`) - the download's GUID is the
    /// document's `AWSTransactionID`, not the document key, and `DocumentManager.publicKey`/`.privateKey`
    /// (static, process-global) must be set from `DMSAPIPublicKey`/`DMSAPIPrivateKey` - decrypted via
    /// the real `EncryptionManagerAWStoDMS.GetDecryptString` (AES/Rijndael, hardcoded key inside the
    /// DLL, confirmed matches the `Encryptionkey` value Zohaib supplied separately) - before every call,
    /// since `DownloadAsync(Guid)` uses whatever these static fields currently hold rather than
    /// resolving them itself. Without this, the DLL builds an unauthenticated request that the real AWS
    /// API rejects with "invalid jwt" - this was the actual root cause of the earlier failures.
    /// </summary>
    public async Task<byte[]?> DownloadFromAwsAsync(AwsDocumentStatus status, CancellationToken ct = default)
    {
        if (!Guid.TryParse(status.AwsTransactionId, out var guid))
        {
            return null;
        }

        DocumentManager.publicKey = status.DmsApiPublicKeyEncrypted?.GetDecryptString() ?? string.Empty;
        DocumentManager.privateKey = status.DmsApiPrivateKeyEncrypted?.GetDecryptString() ?? string.Empty;

        // Constructed fresh per call, after ConfigureAws has run - confirmed live (2026-07-24) that
        // constructing this before SetBaseUrlAndSecretKey bakes in a broken HttpClient (permanently
        // fails with "Handler did not return a response message" regardless of config set afterward).
        var info = await new DocumentManager().DownloadAsync(guid);
        return info?.DocumentStatus == true ? info.DocumentBytes : null;
    }

    public Task<bool> CheckAwsIsEnabledAsync(int practiceId, string connectionString, CancellationToken ct = default) =>
        Task.FromResult(IndiciDMS.CheckAWSIsEnabled(practiceId, connectionString));

    public Task<string?> DocumentGetByDocumentKeyJsonResultAsync(string documentKey, int practiceId, string dmsConnectionString, string pmsConnectionString, CancellationToken ct = default) =>
        Task.FromResult<string?>(IndiciDMS.DocumentGetByDocumentKeyJsonResult(documentKey, practiceId, dmsConnectionString, pmsConnectionString));

    public Task<AwsDocumentStatus?> GetDocumentStatusFromIndiciAsync(string documentKey, int practiceId, string connectionString, CancellationToken ct = default)
    {
        var real = IndiciDMS.GetDocumentStatusFromIndici(documentKey, practiceId, connectionString);
        if (real is null)
        {
            return Task.FromResult<AwsDocumentStatus?>(null);
        }

        return Task.FromResult<AwsDocumentStatus?>(new AwsDocumentStatus(
            real.DocumentID, real.DocumentName, real.DocumentType, real.DocumentData, real.Base64String, real.IsAWS, real.AWSUrl,
            real.AWSTransactionID, real.DMSAPIPublicKey, real.DMSAPIPrivateKey));
    }
}
