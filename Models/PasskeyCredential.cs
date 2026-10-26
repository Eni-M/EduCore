namespace EduCore.Models;

// A passkey (WebAuthn credential) registered to a user. Only public material is stored: the
// private key never leaves the person's device or password manager, so a database leak yields
// nothing that signs in. Phishing-resistant by construction -- the browser binds each signature
// to the site's origin -- which is what NIST SP 800-63B-4 asks AAL2 verifiers to offer.
public class PasskeyCredential
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public byte[] CredentialId { get; set; } = null!;
    public byte[] PublicKey { get; set; } = null!;

    // The authenticator's counter; a value that goes backwards means a cloned credential.
    public uint SignCount { get; set; }

    // What the person called it ("work laptop"), so they can tell which one to remove.
    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
