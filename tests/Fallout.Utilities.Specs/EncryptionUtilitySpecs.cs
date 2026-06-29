using System;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

public class EncryptionUtilitySpecs
{
    [Fact]
    public void V2_RoundTrip()
    {
        var encrypted = EncryptionUtility.Encrypt("hello world", "correct horse battery staple");

        encrypted.Should().StartWith("v2:");
        EncryptionUtility.Decrypt(encrypted, "correct horse battery staple", name: "test")
            .Should().Be("hello world");
    }

    [Fact]
    public void V2_FreshSaltAndNonce_ProduceDifferentCiphertextForSameInput()
    {
        // Random salt + random nonce per encrypt → same plaintext+password produces different
        // ciphertext each time. Regression guard against the v1 deterministic-IV weakness.
        var a = EncryptionUtility.Encrypt("hello", "pw");
        var b = EncryptionUtility.Encrypt("hello", "pw");

        a.Should().NotBe(b);
    }

    [Fact]
    public void V2_WrongPasswordFails()
    {
        var encrypted = EncryptionUtility.Encrypt("hello", "right-password");

        var act = () => EncryptionUtility.Decrypt(encrypted, "wrong-password", name: "test");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void V2_TamperedCiphertextFails()
    {
        // AES-GCM authenticates the ciphertext — any bit-flip after encrypt should fail decrypt.
        // Regression guard against the v1 malleability weakness (CBC with no MAC).
        var encrypted = EncryptionUtility.Encrypt("hello", "pw");
        var blob = Convert.FromBase64String(encrypted["v2:".Length..]);
        blob[^1] ^= 0xFF; // Flip the last byte (in the ciphertext portion).
        var tampered = "v2:" + Convert.ToBase64String(blob);

        var act = () => EncryptionUtility.Decrypt(tampered, "pw", name: "test");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void V2_TamperedTagFails()
    {
        var encrypted = EncryptionUtility.Encrypt("hello", "pw");
        var blob = Convert.FromBase64String(encrypted["v2:".Length..]);
        // Tag sits at bytes [16+12 .. 16+12+16] = [28..44]. Flip a tag byte.
        blob[28] ^= 0xFF;
        var tampered = "v2:" + Convert.ToBase64String(blob);

        var act = () => EncryptionUtility.Decrypt(tampered, "pw", name: "test");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void V1_LegacyDecryptStillWorks()
    {
        // Backward-compat: existing parameters.json files with v1-encrypted values must keep decrypting.
        // Produce a known-good v1 ciphertext via the test-only legacy encrypt helper, then verify the
        // public Decrypt API reads it.
        var v1Ciphertext = EncryptionUtility.EncryptV1Legacy("legacy-value", "legacy-password");

        v1Ciphertext.Should().StartWith("v1:");
        EncryptionUtility.Decrypt(v1Ciphertext, "legacy-password", name: "legacy")
            .Should().Be("legacy-value");
    }

    [Fact]
    public void V1_WrongPasswordFails()
    {
        var v1Ciphertext = EncryptionUtility.EncryptV1Legacy("legacy-value", "right");

        var act = () => EncryptionUtility.Decrypt(v1Ciphertext, "wrong", name: "legacy");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Encrypt_AlwaysEmitsV2()
    {
        // The public Encrypt API must never emit v1 ciphertexts, even though Decrypt still reads them.
        // Guards the "new encrypts upgrade in place" migration story.
        EncryptionUtility.Encrypt("anything", "anything").Should().StartWith("v2:");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("hello world")]
    [InlineData("üñíçødé 🔒")]
    [InlineData("multi\nline\nvalue")]
    public void V2_RoundTripsArbitraryUtf8(string plaintext)
    {
        var encrypted = EncryptionUtility.Encrypt(plaintext, "pw");

        EncryptionUtility.Decrypt(encrypted, "pw", name: "test").Should().Be(plaintext);
    }
}
