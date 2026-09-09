namespace EduCore.Services.Email;

// Every message carries both parts. The HTML is what almost everyone sees; the plain text is
// what a text-only client, a screen reader in plain mode, and most spam filters read -- a
// message with no text alternative scores worse and renders as nothing in the clients that
// refuse HTML.
public record EmailBody(string Text, string Html);
