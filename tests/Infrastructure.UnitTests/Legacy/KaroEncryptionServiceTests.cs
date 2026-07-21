using HekCoreApi.Infrastructure.Legacy.Karo;
using Xunit;

namespace Infrastructure.UnitTests.Legacy;

/// <summary>
/// Proves the ported Rijndael/AES parameters (key, block size, CBC mode, custom Base64 substitution)
/// are internally consistent - encrypting then decrypting recovers the original plaintext. This does
/// not prove byte-for-byte compatibility with the real legacy `EncryptionManager.cs` (that requires a
/// real legacy-encrypted value to test against), only that this port's own round trip is correct.
/// </summary>
public sealed class KaroEncryptionServiceTests
{
    [Theory]
    [InlineData("12345")]
    [InlineData("abc-encounter-id")]
    [InlineData("some/weird+value=here")]
    public void Decrypt_ReturnsOriginal_AfterEncrypt(string plainText)
    {
        var service = new KaroEncryptionService();

        var cipherText = service.Encrypt(plainText);
        var decrypted = service.Decrypt(cipherText);

        Assert.Equal(plainText, decrypted);
        Assert.DoesNotContain('/', cipherText);
        Assert.DoesNotContain('+', cipherText);
        Assert.DoesNotContain('=', cipherText);
    }
}
