using System.Security.Cryptography;
using System.Text;

namespace EduCore.Security.Encryption;

// Field encryption at rest, two schemes:
//
// Deterministic -- enc:{keyId}:BASE64(iv || ciphertext). AES-256-CBC with a synthetic IV: the IV
// is HMAC-SHA256(macKey, plaintext) truncated to a block. Equal plaintexts encrypt equally, which
// the encrypted identifier columns need for their unique indexes and equality lookups. Because the
// IV *is* the MAC, Decrypt recomputes it from the recovered plaintext and refuses a mismatch, so a
// tampered value is rejected rather than silently decrypted -- the SIV construction's integrity.
//
// Randomised -- enc-gcm:{keyId}:BASE64(nonce || tag || ciphertext). AES-256-GCM with a fresh nonce,
// for values nobody searches (medical notes): equal plaintexts encrypt differently, so the
// column reveals nothing, and the tag authenticates.
//
// The key id is in every envelope, so keys rotate: new writes use the current key, every key
// still listed decrypts, and FieldReencryption rewrites old values under the current key. The
// original key's id is "v1", which is why values written before rotation existed
// ("enc:v1:...") need no conversion. Each master key is split by HKDF into separate subkeys per
// purpose, so one raw key is never used for two things.
//
// Anything without an envelope is legacy plaintext from before encryption, returned as-is so it
// still reads; NeedsReencryption reports it so the rewrite can encrypt it.
public sealed class AesFieldEncryptor : IFieldEncryptor
{
    public const string LegacyKeyId = "v1";

    private const string Deterministic = "enc:";
    private const string Randomised = "enc-gcm:";
    private const int IvLength = 16;    // AES block size.
    private const int NonceLength = 12; // AES-GCM.
    private const int TagLength = 16;

    private readonly Dictionary<string, Subkeys> _keys = new(StringComparer.Ordinal);
    private readonly string _currentKeyId;

    private sealed record Subkeys(byte[] Enc, byte[] Mac, byte[] Gcm);

    // A single 32-byte master key under the original id -- the shape before rotation existed.
    public AesFieldEncryptor(byte[] masterKey)
        : this(new Dictionary<string, byte[]> { [LegacyKeyId] = masterKey }, LegacyKeyId)
    {
    }

    public AesFieldEncryptor(IReadOnlyDictionary<string, byte[]> masterKeys, string currentKeyId)
    {
        foreach (var (id, key) in masterKeys)
        {
            if (string.IsNullOrWhiteSpace(id) || !id.All(char.IsLetterOrDigit))
                throw new ArgumentException($"Encryption key id '{id}' must be letters and digits only.");
            if (key is not { Length: 32 })
                throw new ArgumentException($"Encryption key '{id}' must be exactly 32 bytes.");

            _keys[id] = new Subkeys(
                Derive(key, "EduCore.FieldEncryption.v1.enc"),
                Derive(key, "EduCore.FieldEncryption.v1.mac"),
                Derive(key, "EduCore.FieldEncryption.v1.gcm"));
        }

        if (!_keys.ContainsKey(currentKeyId))
            throw new ArgumentException($"The current encryption key id '{currentKeyId}' has no key.");

        _currentKeyId = currentKeyId;
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var keys = _keys[_currentKeyId];
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        Span<byte> iv = stackalloc byte[IvLength];
        SyntheticIv(keys, plainBytes, iv);

        using var aes = Aes.Create();
        aes.Key = keys.Enc;
        var cipher = aes.EncryptCbc(plainBytes, iv, PaddingMode.PKCS7);

        var envelope = new byte[IvLength + cipher.Length];
        iv.CopyTo(envelope);
        cipher.CopyTo(envelope, IvLength);

        return $"{Deterministic}{_currentKeyId}:{Convert.ToBase64String(envelope)}";
    }

    public string Seal(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        var envelope = new byte[NonceLength + TagLength + plainBytes.Length];
        var nonce = envelope.AsSpan(0, NonceLength);
        RandomNumberGenerator.Fill(nonce);

        using var gcm = new AesGcm(_keys[_currentKeyId].Gcm, TagLength);
        gcm.Encrypt(nonce, plainBytes, envelope.AsSpan(NonceLength + TagLength), envelope.AsSpan(NonceLength, TagLength));

        return $"{Randomised}{_currentKeyId}:{Convert.ToBase64String(envelope)}";
    }

    public string Decrypt(string stored)
    {
        if (string.IsNullOrEmpty(stored))
            return stored;

        if (stored.StartsWith(Randomised, StringComparison.Ordinal))
            return Open(stored);

        if (!stored.StartsWith(Deterministic, StringComparison.Ordinal))
            return stored; // Legacy plaintext written before encryption was enabled.

        var (keys, envelope) = Unwrap(stored, Deterministic);
        if (envelope.Length < IvLength * 2)
            throw new CryptographicException("Encrypted value is malformed.");

        var iv = envelope.AsSpan(0, IvLength);

        using var aes = Aes.Create();
        aes.Key = keys.Enc;
        var plainBytes = aes.DecryptCbc(envelope.AsSpan(IvLength), iv, PaddingMode.PKCS7);

        Span<byte> expected = stackalloc byte[IvLength];
        SyntheticIv(keys, plainBytes, expected);
        if (!CryptographicOperations.FixedTimeEquals(expected, iv))
            throw new CryptographicException("Encrypted value failed its integrity check.");

        return Encoding.UTF8.GetString(plainBytes);
    }

    public bool NeedsReencryption(string? stored) =>
        !string.IsNullOrEmpty(stored)
        && !stored.StartsWith($"{Deterministic}{_currentKeyId}:", StringComparison.Ordinal)
        && !stored.StartsWith($"{Randomised}{_currentKeyId}:", StringComparison.Ordinal);

    private string Open(string stored)
    {
        var (keys, envelope) = Unwrap(stored, Randomised);
        if (envelope.Length < NonceLength + TagLength)
            throw new CryptographicException("Encrypted value is malformed.");

        var plainBytes = new byte[envelope.Length - NonceLength - TagLength];
        using var gcm = new AesGcm(keys.Gcm, TagLength);
        gcm.Decrypt(
            envelope.AsSpan(0, NonceLength),
            envelope.AsSpan(NonceLength + TagLength),
            envelope.AsSpan(NonceLength, TagLength),
            plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private (Subkeys Keys, byte[] Envelope) Unwrap(string stored, string scheme)
    {
        var rest = stored[scheme.Length..];
        var colon = rest.IndexOf(':');
        if (colon <= 0)
            throw new CryptographicException("Encrypted value is malformed.");

        var keyId = rest[..colon];
        if (!_keys.TryGetValue(keyId, out var keys))
            throw new CryptographicException(
                $"Encrypted value uses key '{keyId}', which is not configured. Add it to Encryption:PreviousKeys.");

        return (keys, Convert.FromBase64String(rest[(colon + 1)..]));
    }

    private static void SyntheticIv(Subkeys keys, ReadOnlySpan<byte> plainBytes, Span<byte> iv)
    {
        Span<byte> mac = stackalloc byte[32];
        HMACSHA256.HashData(keys.Mac, plainBytes, mac);
        mac[..IvLength].CopyTo(iv);
    }

    private static byte[] Derive(byte[] masterKey, string purpose) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, outputLength: 32, salt: null,
            info: Encoding.UTF8.GetBytes(purpose));
}
