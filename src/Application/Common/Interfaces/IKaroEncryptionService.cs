namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from legacy-reference/hsswebapi/DevLocal/HSSWebAPI/Models/EncryptionManager.cs -
/// KARO/HSS's real reversible encryption used for `patientId`/`encounterId` values on the wire.
/// Must be byte-for-byte compatible with the real legacy scheme (same key, same cipher parameters,
/// same custom Base64 substitution) so real callers' already-encrypted values decrypt correctly.
/// </summary>
public interface IKaroEncryptionService
{
    /// <summary>Legacy: `EncryptionManager.GetDecryptString`. Returns empty string on any failure (matches legacy's silent-swallow behavior).</summary>
    string Decrypt(string cipherText);

    /// <summary>Legacy: `EncryptionManager.GetEncryptedString`. Returns empty string on any failure (matches legacy's silent-swallow behavior).</summary>
    string Encrypt(string clearText);
}
