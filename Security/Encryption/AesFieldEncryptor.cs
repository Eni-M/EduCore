using System.Security.Cryptography;
using System.Text;

namespace EduCore.Security.Encryption;

/// <summary>
/// Deterministic AES-256-CBC field encryption with a synthetic (HMAC-derived) IV.
///
/// Determinism is a hard requirement here: the encrypted columns (<c>Student.PersonalNumber</c>,
/// <c>Guardian.NationalId</c>) carry a unique index and are located by equality lookups, so the
/// same plaintext must always map to the same ciphertext. A random IV would break both.
///
/// The IV is derived as HMAC-SHA256(macKey, plaintext) truncated to the AES block size, following
/// the synthetic-IV principle: equal plaintexts get equal IVs (determinism preserved), while
/// distinct plaintexts get distinct IVs, so CBC never leaks equality of shared prefixes across
/// records. The master key is split into independent encryption and MAC subkeys via HKDF, so the
/// same raw key is never used for two purposes.
///
/// The envelope is <c>enc:v1:BASE64(iv || ciphertext)</c>. The version prefix lets us evolve the
/// scheme and lets <see cref="Decrypt"/> pass through legacy plaintext that predates encryption.
/// </summary>
public sealed class AesFieldEncryptor : IFieldEncryptor
{
    private const string Prefix = "enc:v1:";
    private const int IvLength = 16; // AES block size.

    private readonly byte[] _encKey;
    private readonly byte[] _macKey;

    /// <param name="masterKey">A 32-byte master key (e.g. the base64-decoded <c>Encryption:Key</c>).</param>
    public AesFieldEncryptor(byte[] masterKey)
    {
        ArgumentNullException.ThrowIfNull(masterKey);
        if (masterKey.Length != 32)
            throw new ArgumentException("The encryption master key must be exactly 32 bytes.", nameof(masterKey));

        _encKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, masterKey, outputLength: 32,
            salt: null, info: "EduCore.FieldEncryption.v1.enc"u8.ToArray());
        _macKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, masterKey, outputLength: 32,
            salt: null, info: "EduCore.FieldEncryption.v1.mac"u8.ToArray());
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        Span<byte> iv = stackalloc byte[IvLength];
        using (var hmac = new HMACSHA256(_macKey))
        {
            Span<byte> mac = stackalloc byte[32];
            hmac.TryComputeHash(plainBytes, mac, out _);
            mac[..IvLength].CopyTo(iv);
        }

        using var aes = Aes.Create();
        aes.Key = _encKey;
        var cipher = aes.EncryptCbc(plainBytes, iv, PaddingMode.PKCS7);

        var envelope = new byte[IvLength + cipher.Length];
        iv.CopyTo(envelope);
        cipher.CopyTo(envelope, IvLength);

        return Prefix + Convert.ToBase64String(envelope);
    }

    public string Decrypt(string stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal))
            return stored; // Empty, or legacy plaintext written before encryption was enabled.

        var envelope = Convert.FromBase64String(stored[Prefix.Length..]);
        if (envelope.Length < IvLength)
            throw new CryptographicException("Encrypted value is malformed.");

        var iv = envelope.AsSpan(0, IvLength);
        var cipher = envelope.AsSpan(IvLength);

        using var aes = Aes.Create();
        aes.Key = _encKey;
        var plainBytes = aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
