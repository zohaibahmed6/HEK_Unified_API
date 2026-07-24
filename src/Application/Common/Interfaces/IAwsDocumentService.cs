namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// v1.1 spec follow-through, Step 5: real `AWSDoc.IndiciDMS` shapes, confirmed by reflecting the real
/// `AWSDocCore.dll` Zohaib supplied (not guessed) - corrects the earlier interface, which assumed no
/// connection-string parameters and a `string?` return from `GetDocumentStatusFromIndici` (the real
/// method returns a structured status object, see <see cref="AwsDocumentStatus"/>). All three real
/// methods take a connection string directly rather than resolving one internally - callers pass the
/// same per-practice connection <see cref="ILegacyPracticeConnectionResolver.ResolveAsync(string, CancellationToken)"/>
/// already resolves everywhere else, per Zohaib's direction (2026-07-24) rather than a separate lookup.
/// </summary>
public interface IAwsDocumentService
{
    /// <summary>Legacy: `AWSDoc.IndiciDMS.CheckAWSIsEnabled(practiceId, connectionString)` - called unconditionally on every HISO procedure execution.</summary>
    Task<bool> CheckAwsIsEnabledAsync(int practiceId, string connectionString, CancellationToken ct = default);

    /// <summary>Legacy: `AWSDoc.IndiciDMS.DocumentGetByDocumentKeyJsonResult(docKey, practiceId, dmsConnectionString, pmsConnectionString)` - only called when a non-empty reference ID is present.</summary>
    Task<string?> DocumentGetByDocumentKeyJsonResultAsync(string documentKey, int practiceId, string dmsConnectionString, string pmsConnectionString, CancellationToken ct = default);

    /// <summary>Legacy: `AWSDoc.IndiciDMS.GetDocumentStatusFromIndici(docKey, practiceId, connectionString)` - always called, used for MIME/status enrichment.</summary>
    Task<AwsDocumentStatus?> GetDocumentStatusFromIndiciAsync(string documentKey, int practiceId, string connectionString, CancellationToken ct = default);

    /// <summary>
    /// v1.1 spec follow-through (2026-07-24): real `AWSDocCore.DocumentManager.DownloadAsync(Guid)` -
    /// the actual S3 content fetch. Confirmed via reflection that `DocumentGetByDocumentKeyJsonResult`
    /// only ever returns metadata (`DocumentData` is always null there) - real file bytes for
    /// AWS-flagged documents require this separate call against the real DMS AWS REST API, which itself
    /// requires `ConfigureAws` to have been called first with the real base URL/secret key.
    /// </summary>
    Task<byte[]?> DownloadFromAwsAsync(AwsDocumentStatus status, CancellationToken ct = default);

    /// <summary>Legacy: `AWSDocCore.DocumentManager.SetBaseUrlAndSecretKey(baseUrl, secretKey, jwtExpiryMinutes, s3TimeoutSeconds)` - must be called once before <see cref="DownloadFromAwsAsync"/> can succeed.</summary>
    void ConfigureAws(string baseUrl, string secretKey, int jwtTokenExpiryMinutes, int s3RequestTimeoutSeconds);
}

/// <summary>Application-layer projection of the real `AWSDocCore.DataModel.DocumentFileStatus` shape.
/// v1.1 spec follow-through (2026-07-24): confirmed via the real `AWSDocCore` source (Zohaib supplied
/// the source project path) that `AWSTransactionID`/`DMSAPIPublicKey`/`DMSAPIPrivateKey` are required
/// for the real S3 download - `DocumentManager.Download` parses `AWSTransactionID` (not the document
/// key) as the download's GUID, and decrypts the two DMSAPI keys via `EncryptionManagerAWStoDMS` before
/// building the request's auth. Without these three, the real DLL builds an unauthenticated/malformed
/// request that the AWS API rejects - this was the actual root cause of the "invalid jwt" failures.</summary>
public sealed record AwsDocumentStatus(
    int DocumentId,
    string? DocumentName,
    string? DocumentType,
    byte[]? DocumentData,
    string? Base64String,
    bool IsAws,
    string? AwsUrl,
    string? AwsTransactionId,
    string? DmsApiPublicKeyEncrypted,
    string? DmsApiPrivateKeyEncrypted);
