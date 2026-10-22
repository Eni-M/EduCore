namespace EduCore.Security.Encryption;

// Encrypts and decrypts sensitive string fields for storage at rest.
public interface IFieldEncryptor
{
    // Deterministic: the same plaintext always yields the same ciphertext, so an encrypted
    // column can still back a unique index and be found by an equality lookup.
    string Encrypt(string plaintext);

    // Randomised and authenticated, for values that are never searched (medical notes).
    string Seal(string plaintext);

    // Decrypts a value produced by either method. A value that is not one of our envelopes
    // (legacy plaintext written before encryption was enabled) is returned as-is.
    string Decrypt(string stored);

    // True when the stored value is legacy plaintext or was written under a key that is no
    // longer the current one -- what the re-encryption pass rewrites.
    bool NeedsReencryption(string? stored);
}
