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
}
