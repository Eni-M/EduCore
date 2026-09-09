namespace EduCore.Security.Encryption;

// Encrypts and decrypts sensitive string fields (e.g. national identifiers) for storage at rest.
// Encryption is deterministic: the same plaintext always yields the same ciphertext, so an
// encrypted column can still back a unique index and be found by an equality lookup.
public interface IFieldEncryptor
{
    // Encrypts plaintext into a self-describing, versioned envelope.
    string Encrypt(string plaintext);

    // Decrypts a value produced by Encrypt. A value that is not one of our
    // envelopes (e.g. legacy plaintext written before encryption was enabled) is returned as-is.
    string Decrypt(string stored);
}
