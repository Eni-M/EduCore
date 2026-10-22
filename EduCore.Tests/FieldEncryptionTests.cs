using System.Security.Cryptography;
using EduCore.Security.Encryption;
using Xunit;

namespace EduCore.Tests;

public class FieldEncryptionTests
{
    private static AesFieldEncryptor NewEncryptor(byte[]? key = null) =>
        new(key ?? RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void Encrypt_then_decrypt_round_trips()
    {
        var enc = NewEncryptor();

        var cipher = enc.Encrypt("1234567890123");

        Assert.NotEqual("1234567890123", cipher);
        Assert.StartsWith("enc:v1:", cipher);
        Assert.Equal("1234567890123", enc.Decrypt(cipher));
    }

    [Fact]
    public void Encryption_is_deterministic_so_unique_indexes_and_lookups_still_work()
    {
        var key = RandomNumberGenerator.GetBytes(32);

        // Same key, two separate instances: the same plaintext must produce the same ciphertext,
        // otherwise the unique index and equality lookups on the encrypted column would break.
        Assert.Equal(NewEncryptor(key).Encrypt("AB9988"), NewEncryptor(key).Encrypt("AB9988"));
    }

    [Fact]
    public void Distinct_plaintexts_produce_distinct_ciphertexts()
    {
        var enc = NewEncryptor();
        Assert.NotEqual(enc.Encrypt("AB9988"), enc.Encrypt("AB9989"));
    }

    [Fact]
    public void Different_keys_cannot_read_each_others_values()
    {
        var cipher = NewEncryptor().Encrypt("secret-id");
        Assert.ThrowsAny<CryptographicException>(() => NewEncryptor().Decrypt(cipher));
    }

    [Fact]
    public void Decrypt_passes_through_legacy_plaintext_and_empty()
    {
        var enc = NewEncryptor();
        Assert.Equal("legacy-plain", enc.Decrypt("legacy-plain"));
        Assert.Equal("", enc.Decrypt(""));
    }

    [Fact]
    public void Constructor_rejects_a_wrong_sized_key()
    {
        Assert.Throws<ArgumentException>(() => new AesFieldEncryptor(new byte[16]));
    }

    // The IV is the MAC of the plaintext, so changing any ciphertext byte is caught on decrypt
    // instead of yielding a different plaintext.
    [Fact]
    public void A_tampered_ciphertext_is_rejected()
    {
        var encryptor = NewEncryptor();
        var stored = encryptor.Encrypt("1234567890");
        var prefix = stored[..stored.LastIndexOf(':')];
        var bytes = Convert.FromBase64String(stored[(prefix.Length + 1)..]);
        bytes[^1] ^= 0x01;
        bytes[^17] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(
            () => encryptor.Decrypt($"{prefix}:{Convert.ToBase64String(bytes)}"));
    }

    [Fact]
    public void Values_written_before_rotation_keep_their_original_envelope()
    {
        Assert.StartsWith("enc:v1:", NewEncryptor().Encrypt("1234567890"));
    }

    [Fact]
    public void After_rotation_old_values_still_decrypt_and_are_flagged_for_rewrite()
    {
        var oldKey = RandomNumberGenerator.GetBytes(32);
        var stored = NewEncryptor(oldKey).Encrypt("1234567890");

        var rotated = new AesFieldEncryptor(
            new Dictionary<string, byte[]> { ["v1"] = oldKey, ["v2"] = RandomNumberGenerator.GetBytes(32) }, "v2");

        Assert.Equal("1234567890", rotated.Decrypt(stored));
        Assert.True(rotated.NeedsReencryption(stored));
        Assert.StartsWith("enc:v2:", rotated.Encrypt("1234567890"));
        Assert.False(rotated.NeedsReencryption(rotated.Encrypt("1234567890")));
    }

    [Fact]
    public void Legacy_plaintext_is_flagged_for_rewrite()
    {
        Assert.True(NewEncryptor().NeedsReencryption("1234567890"));
        Assert.False(NewEncryptor().NeedsReencryption(null));
    }

    // Medical notes: never searched, so equal values must not look equal at rest.
    [Fact]
    public void Sealed_values_are_randomised_authenticated_and_round_trip()
    {
        var encryptor = NewEncryptor();
        var first = encryptor.Seal("Peanut allergy");
        var second = encryptor.Seal("Peanut allergy");

        Assert.NotEqual(first, second);
        Assert.Equal("Peanut allergy", encryptor.Decrypt(first));

        var prefix = first[..first.LastIndexOf(':')];
        var bytes = Convert.FromBase64String(first[(prefix.Length + 1)..]);
        bytes[^1] ^= 0x01;
        Assert.ThrowsAny<CryptographicException>(
            () => encryptor.Decrypt($"{prefix}:{Convert.ToBase64String(bytes)}"));
    }
}
