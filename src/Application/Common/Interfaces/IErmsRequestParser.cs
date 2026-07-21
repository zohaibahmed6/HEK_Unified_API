namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Shared real ERMS parsing logic - like `IKaroRequestParser` but with ERMS's real extra outer
/// `GetBase64Value` unwrap (plain Base64, numeric passthrough) applied to `encounterId`/`patientId`
/// before the same real `"__"/"_"` split, and the real `pho = splitEncounter[3]` capture (only in the
/// length&gt;3 branch) that KARO's equivalent parsing does not have.
/// </summary>
public interface IErmsRequestParser
{
    /// <summary>
    /// `RawSecondSegment` is the undecorated `splitEncounter[1]` - legacy's report/scanned ops convert it
    /// with `Convert.ToInt32` into `actualPracticeID` (throwing on non-numeric, a quirk callers preserve);
    /// null when the encounterId has no underscore (legacy leaves `actualPracticeID` at 0).
    /// </summary>
    (string? EncounterId, string PracticeSuffix, string? Pho, string? RawSecondSegment) ParseEncounterId(string? encounterId);

    /// <summary>Legacy: `GetBase64Value` - plain Base64 decode, numeric passthrough, returns the original value unchanged on any decode failure.</summary>
    string? DecodeBase64(string? value);

    /// <summary>Legacy: `GetDcrptValue` - numeric passthrough, else real AES decrypt (same as KARO's).</summary>
    string? Decrypt(string? value);
}
