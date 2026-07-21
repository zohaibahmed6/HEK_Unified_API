namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// The 3 real `AWSDoc.IndiciDMS` calls confirmed from legacy source
/// (legacy-reference/Hiso/DAL/DBMessages.cs's `ExecuteAWSFlow`/`EnrichWithAWS`). `AWSDoc.IndiciDMS`
/// itself is a compiled DLL Zohaib will map in later, not source-portable - this interface exists so
/// the calling-code orchestration (enable-check branching, `_AWS` procedure-suffix convention, field
/// derivation, silent fallback-to-plain-procedure on failure) can be built for real now, and wiring
/// the real DLL later is a drop-in implementation, not a rebuild. Until then, a no-op implementation
/// registered in DI always reports AWS disabled, matching the safe default (falls through to the
/// plain, non-AWS procedure path - the same behavior this project already had before this interface
/// existed).
/// </summary>
public interface IAwsDocumentService
{
    /// <summary>Legacy: `AWSDoc.IndiciDMS.CheckAWSIsEnabled(practiceId)` - called unconditionally on every HISO procedure execution.</summary>
    Task<bool> CheckAwsIsEnabledAsync(int practiceId, CancellationToken ct = default);

    /// <summary>Legacy: `AWSDoc.IndiciDMS.DocumentGetByDocumentKeyJsonResult(docKey, practiceId)` - only called when a non-empty reference ID is present.</summary>
    Task<string?> DocumentGetByDocumentKeyJsonResultAsync(string documentKey, int practiceId, CancellationToken ct = default);

    /// <summary>Legacy: `AWSDoc.IndiciDMS.GetDocumentStatusFromIndici(docKey, practiceId)` - always called, used for MIME/status enrichment.</summary>
    Task<string?> GetDocumentStatusFromIndiciAsync(string documentKey, int practiceId, CancellationToken ct = default);
}
