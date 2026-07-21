namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// COL/Pegasus's real encounterId parsing (`COLController.cs`) - differs from ERMS's: NO base64
/// unwrap, and when the split has &gt;2 segments the practice suffix is <b>overwritten</b> with
/// `"_" + splitEncounter[2]` (not appended); no pho capture. Decrypt is the same numeric-passthrough
/// `GetDcrptValue` (shared `EncryptionManager`, same key as ERMS/KARO).
/// </summary>
public interface IColRequestParser
{
    (string? EncounterId, string PracticeSuffix) ParseEncounterId(string? encounterId);

    string? Decrypt(string? value);
}
