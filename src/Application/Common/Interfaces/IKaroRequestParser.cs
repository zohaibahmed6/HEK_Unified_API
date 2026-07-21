namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Shared real KARO parsing logic (`APIController.cs`'s identical copy-pasted encounterId/practiceid
/// split + patientId/encounterId decryption block, present in every operation). Extracted once here
/// rather than duplicated per-operation - behavior is byte-identical to legacy, only the code location
/// differs.
/// </summary>
public interface IKaroRequestParser
{
    /// <summary>
    /// Legacy: the real encounterId "__"/"_ " split + GetDcrptValue decrypt logic, shared by every
    /// operation including Authenticate. <c>PracticeSuffixNumeric</c> is the raw second split segment
    /// alone (legacy's `practiceid2` in `GetPatientAttachment`/`SaveDocument` - captured before any
    /// later segments are appended to <c>PracticeSuffix</c>, so it stays purely numeric even when
    /// <c>PracticeSuffix</c> ends up compound, e.g. "_901_FZZ999-B").
    /// </summary>
    (string? EncounterId, string PracticeSuffix, string PracticeSuffixNumeric) ParseEncounterId(string? encounterId);

    /// <summary>Legacy: `GetDcrptValue` - numeric values pass through unchanged, everything else is decrypted.</summary>
    string? Decrypt(string? value);
}
