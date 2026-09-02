namespace EduCore.Security.Encryption;

/// <summary>
/// Encrypts and decrypts sensitive string fields (e.g. national identifiers) for storage at rest.
/// Encryption is deterministic: the same plaintext always yields the same ciphertext, so an
/// encrypted column can still back a unique index and be found by an equality lookup.
/// </summary>
public interface IFieldEncryptor
{
    /// <summary>Encrypts <paramref name="plaintext"/> into a self-describing, versioned envelope.</summary>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a value produced by <see cref="Encrypt"/>. A value that is not one of our
    /// envelopes (e.g. legacy plaintext written before encryption was enabled) is returned as-is.
    /// </summary>
    string Decrypt(string stored);
}
