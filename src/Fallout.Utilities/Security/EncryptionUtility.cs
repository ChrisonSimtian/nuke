#if NET6_0_OR_GREATER

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fallout.Common.Utilities;

/// <summary>
/// Symmetric encryption for the <c>fallout :secrets</c> flow. Two ciphertext formats are recognised:
///
/// <list type="bullet">
///   <item>
///     <description><b>v2 (current)</b>: AES-GCM with random 16-byte salt + random 12-byte nonce per
///     encrypt, PBKDF2-SHA256 at 600,000 iterations (OWASP 2023). Authenticated — tampered ciphertext
///     is rejected by the GCM auth tag. Format: <c>v2:base64(salt[16] || nonce[12] || tag[16] || ciphertext)</c>.
///     All new encrypts emit v2.</description>
///   </item>
///   <item>
///     <description><b>v1 (legacy)</b>: AES-CBC with static salt (<c>"Ivan Medvedev"</c>) and
///     KDF-derived IV, PBKDF2-SHA256 at 10,000 iterations. Unauthenticated. Read-only — kept for
///     backward-compatible decryption of values committed under earlier versions. Re-saving any
///     secret via <c>fallout :secrets</c> migrates it to v2.</description>
///   </item>
/// </list>
///
/// See <see href="https://github.com/Fallout-build/Fallout/issues/212">#212</see> for the security
/// audit that motivated the v2 format.
/// </summary>
internal static class EncryptionUtility
{
    private const string V1Prefix = "v1:";
    private const string V2Prefix = "v2:";

    private const int V2SaltLength = 16;
    private const int V2NonceLength = 12;
    private const int V2TagLength = 16;
    private const int V2KeyLength = 32;
    private const int V2Iterations = 600_000;

    // Legacy v1 constants — preserved verbatim so we can still decrypt values committed under the
    // old algorithm. New encrypts never use these.
    private const int V1KeyLength = 32;
    private const int V1IvLength = 16;
    private const int V1Iterations = 10_000;
    private static readonly byte[] s_v1StaticSalt = { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };

    public static string Decrypt(string cipherText, string password, string name)
    {
        try
        {
            if (cipherText.StartsWith(V2Prefix, StringComparison.Ordinal))
                return DecryptV2(cipherText[V2Prefix.Length..], password);
            if (cipherText.StartsWith(V1Prefix, StringComparison.Ordinal))
                return DecryptV1(cipherText[V1Prefix.Length..], password);
            // Unprefixed ciphertext from very old setups — treat as v1.
            return DecryptV1(cipherText, password);
        }
        catch
        {
            Assert.Fail($"Could not decrypt '{name}' with provided password");
            return null;
        }
    }

    public static string Encrypt(string clearText, string password)
    {
        var plaintext = Encoding.UTF8.GetBytes(clearText);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var salt = new byte[V2SaltLength];
        RandomNumberGenerator.Fill(salt);
        var nonce = new byte[V2NonceLength];
        RandomNumberGenerator.Fill(nonce);

        var key = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, V2Iterations, HashAlgorithmName.SHA256, V2KeyLength);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[V2TagLength];
        using (var aes = new AesGcm(key, V2TagLength))
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var blob = new byte[V2SaltLength + V2NonceLength + V2TagLength + ciphertext.Length];
        Buffer.BlockCopy(salt, 0, blob, 0, V2SaltLength);
        Buffer.BlockCopy(nonce, 0, blob, V2SaltLength, V2NonceLength);
        Buffer.BlockCopy(tag, 0, blob, V2SaltLength + V2NonceLength, V2TagLength);
        Buffer.BlockCopy(ciphertext, 0, blob, V2SaltLength + V2NonceLength + V2TagLength, ciphertext.Length);

        return V2Prefix + Convert.ToBase64String(blob);
    }

    public static string GetGeneratedPassword(int bits = 256)
    {
        var password = new byte[bits / 8];
        RandomNumberGenerator.Fill(password);
        return Convert.ToBase64String(password);
    }

    private static string DecryptV2(string base64Blob, string password)
    {
        var blob = Convert.FromBase64String(base64Blob);
        Assert.True(blob.Length >= V2SaltLength + V2NonceLength + V2TagLength, "v2 ciphertext blob is too short");

        var salt = new byte[V2SaltLength];
        Buffer.BlockCopy(blob, 0, salt, 0, V2SaltLength);
        var nonce = new byte[V2NonceLength];
        Buffer.BlockCopy(blob, V2SaltLength, nonce, 0, V2NonceLength);
        var tag = new byte[V2TagLength];
        Buffer.BlockCopy(blob, V2SaltLength + V2NonceLength, tag, 0, V2TagLength);
        var ciphertextLength = blob.Length - V2SaltLength - V2NonceLength - V2TagLength;
        var ciphertext = new byte[ciphertextLength];
        Buffer.BlockCopy(blob, V2SaltLength + V2NonceLength + V2TagLength, ciphertext, 0, ciphertextLength);

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var key = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, V2Iterations, HashAlgorithmName.SHA256, V2KeyLength);

        var plaintext = new byte[ciphertextLength];
        using (var aes = new AesGcm(key, V2TagLength))
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static string DecryptV1(string base64Ciphertext, string password)
    {
        var ciphertext = Convert.FromBase64String(base64Ciphertext);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var keyAndIv = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, s_v1StaticSalt, V1Iterations, HashAlgorithmName.SHA256, V1KeyLength + V1IvLength);
        var key = new byte[V1KeyLength];
        var iv = new byte[V1IvLength];
        Buffer.BlockCopy(keyAndIv, 0, key, 0, V1KeyLength);
        Buffer.BlockCopy(keyAndIv, V1KeyLength, iv, 0, V1IvLength);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
            cryptoStream.Write(ciphertext, 0, ciphertext.Length);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    /// <summary>
    /// Test-only: produces a v1-formatted ciphertext using the legacy algorithm. Exposed via
    /// InternalsVisibleTo so backward-compatibility tests can verify <see cref="Decrypt"/> still
    /// reads historical values without depending on hardcoded ciphertext strings. Production code
    /// must never call this — new encrypts must always go through <see cref="Encrypt"/> (v2).
    /// </summary>
    internal static string EncryptV1Legacy(string clearText, string password)
    {
        var plaintext = Encoding.UTF8.GetBytes(clearText);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var keyAndIv = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, s_v1StaticSalt, V1Iterations, HashAlgorithmName.SHA256, V1KeyLength + V1IvLength);
        var key = new byte[V1KeyLength];
        var iv = new byte[V1IvLength];
        Buffer.BlockCopy(keyAndIv, 0, key, 0, V1KeyLength);
        Buffer.BlockCopy(keyAndIv, V1KeyLength, iv, 0, V1IvLength);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            cryptoStream.Write(plaintext, 0, plaintext.Length);

        return V1Prefix + Convert.ToBase64String(memoryStream.ToArray());
    }
}
#endif
