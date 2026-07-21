using System.Security.Cryptography;
using System.Text;
using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>
/// Exact port of legacy-reference/hsswebapi/DevLocal/HSSWebAPI/Models/EncryptionManager.cs.
/// Legacy used `RijndaelManaged` with its default 128-bit block size and a 256-bit (32-byte) key in
/// CBC mode - .NET's <see cref="Aes"/> class uses the same defaults, so it is call-compatible without
/// any external dependency. Ciphertext format (confirmed from source): standard Base64 of
/// `IV (16 bytes) + encrypted bytes`, then a custom URL-safe substitution applied on top of Base64:
/// '/'→'_', '+'→'-', '='→'£' (and the reverse before Base64-decoding). The 32-byte key is copied
/// verbatim from the real legacy source - this is what makes ciphertexts produced by the real legacy
/// system decryptable here, and vice versa.
/// </summary>
public sealed class KaroEncryptionService : IKaroEncryptionService
{
    private static readonly byte[] Key =
    {
        137, 10, 237, 69, 157, 169, 181, 70, 216, 110, 47, 209, 193, 153, 196, 109,
        25, 146, 165, 140, 128, 7, 175, 122, 34, 247, 157, 143, 54, 233, 124, 219
    };

    public string Encrypt(string clearText)
    {
        if (string.IsNullOrWhiteSpace(clearText))
        {
            return string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt, new UTF8Encoding(false)))
            {
                swEncrypt.Write(clearText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray())
                .Replace('/', '_').Replace('+', '-').Replace('=', '£');
        }
        catch
        {
            return string.Empty;
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return string.Empty;
        }

        try
        {
            var normalized = cipherText.Replace("_", "/").Replace("-", "+").Replace("£", "=");
            var bytes = Convert.FromBase64String(normalized);

            var iv = new byte[16];
            Array.Copy(bytes, 0, iv, 0, iv.Length);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.Mode = CipherMode.CBC;

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            using var msDecrypt = new MemoryStream(bytes, iv.Length, bytes.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt, new UTF8Encoding(false));
            return srDecrypt.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }
}
