namespace EduCore.Services.Email;

// An enum rather than pre-rendered text, so the queue carries facts and the wording is decided
// at composition time.
public enum AccountMailKind
{
    Welcome,

    // Sent for every change, whoever made it.
    PasswordChanged,

    // To the address being left behind, so a hijacked session moving the account away is noticed.
    EmailChanged,

    // Queued rather than sent inline so a reset request takes as long whether or not the address
    // holds an account; the reply time must not say which.
    PasswordReset,

    // An authenticator app was added to or removed from the account. Sent either way: an app the
    // owner did not add is how a hijacked session would lock them out.
    AuthenticatorAdded,
    AuthenticatorRemoved
}

// Copied at the moment of the write, never the User entity -- its DbContext is long disposed by
// the time the background sender runs.
// Link is set only for PasswordReset (single-use, short-lived), NewEmail only for EmailChanged.
public record AccountMailRequest(
    AccountMailKind Kind,
    string Email,
    string FirstName,
    string? Link = null,
    int? ExpiresInMinutes = null,
    string? NewEmail = null);

// Composes account emails as pure functions, so the wording is testable without a mail server.
// No message carries a password. The reset message is the one that carries a token, in the link
// the user asked for; it is single-use and time-limited.
//
// Each message is written once as a list of blocks; EmailLayout renders that to HTML and to
// plain text, so the two cannot drift apart.
public static class AccountMailTemplates
{
    // Derived from the reset link's origin rather than configured twice: two settings for one
    // fact drift, and the symptom is mail pointing somewhere nobody can reach.
    public static string AppUrlFrom(string resetUrlTemplate) =>
        Uri.TryCreate(resetUrlTemplate, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : resetUrlTemplate;

    public static (string Subject, EmailBody Body) Compose(AccountMailRequest request, string appUrl) =>
        request.Kind switch
        {
            AccountMailKind.Welcome => Welcome(request, appUrl),
            AccountMailKind.PasswordChanged => PasswordChanged(request, appUrl),
            AccountMailKind.EmailChanged => EmailChanged(request, appUrl),
            AccountMailKind.PasswordReset => PasswordReset(request, appUrl),
            AccountMailKind.AuthenticatorAdded => AuthenticatorChanged(request, appUrl, added: true),
            AccountMailKind.AuthenticatorRemoved => AuthenticatorChanged(request, appUrl, added: false),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

    private static (string, EmailBody) Welcome(AccountMailRequest request, string appUrl) =>
        ("Your EduCore account is ready",
            EmailLayout.Render(
                $"Welcome, {request.FirstName}",
                [
                    new EmailBlock.Paragraph(
                        "Your school has created an EduCore account for you. You sign in with this email address:"),
                    new EmailBlock.Code(request.Email, Spaced: false),
                    new EmailBlock.Paragraph(
                        "Your school will give you a temporary password. You will be asked to choose your own the first time you sign in."),
                    new EmailBlock.Button("Sign in", appUrl),
                    new EmailBlock.Note(
                        "If nobody has given you a password, use the \"Forgot your password?\" link on the sign-in page and we will email you one that works once."),
                ],
                appUrl));

    private static (string, EmailBody) EmailChanged(AccountMailRequest request, string appUrl) =>
        ("Your EduCore sign-in email was changed",
            EmailLayout.Render(
                "Your sign-in email was changed",
                [
                    new EmailBlock.Paragraph($"Hello {request.FirstName},"),
                    new EmailBlock.Paragraph(
                        "The email address you sign in to EduCore with has just been changed. From now on, sign-in codes and password resets go to:"),
                    new EmailBlock.Code(request.NewEmail ?? "", Spaced: false),
                    new EmailBlock.Paragraph("If you made this change, there is nothing to do."),
                    new EmailBlock.Paragraph(
                        "If you did not, someone else has access to your account. Contact your school office straight away."),
                ],
                appUrl));

    private static (string, EmailBody) AuthenticatorChanged(AccountMailRequest request, string appUrl, bool added) =>
        (added ? "An authenticator app was added to your EduCore account" : "An authenticator app was removed from your EduCore account",
            EmailLayout.Render(
                added ? "Authenticator app added" : "Authenticator app removed",
                [
                    new EmailBlock.Paragraph($"Hello {request.FirstName},"),
                    new EmailBlock.Paragraph(added
                        ? "An authenticator app was just set up for your EduCore account. Sign-in codes now come from that app instead of email."
                        : "The authenticator app on your EduCore account was just removed. Sign-in codes now come by email again."),
                    new EmailBlock.Paragraph("If you made this change, there is nothing to do."),
                    new EmailBlock.Paragraph(
                        "If you did not, contact your school office straight away. They can reset your account, which also removes any authenticator app."),
                ],
                appUrl));

    private static (string, EmailBody) PasswordReset(AccountMailRequest request, string appUrl) =>
        ("Reset your EduCore password",
            EmailLayout.Render(
                "Reset your password",
                [
                    new EmailBlock.Paragraph($"Hello {request.FirstName},"),
                    new EmailBlock.Paragraph(
                        "Someone asked to reset the password for your EduCore account. Use the button "
                        + $"below to choose a new one. It works once, and expires in {request.ExpiresInMinutes} minutes."),
                    new EmailBlock.Button("Choose a new password", request.Link ?? appUrl),
                    new EmailBlock.Note(
                        "If this wasn't you, ignore this email -- your password has not changed."),
                ],
                appUrl));

    private static (string, EmailBody) PasswordChanged(AccountMailRequest request, string appUrl) =>
        ("Your EduCore password was changed",
            EmailLayout.Render(
                "Your password was changed",
                [
                    new EmailBlock.Paragraph($"Hello {request.FirstName},"),
                    new EmailBlock.Paragraph(
                        $"The password for your EduCore account ({request.Email}) has just been changed."),
                    new EmailBlock.Paragraph("If you made this change, there is nothing to do."),
                    new EmailBlock.Paragraph(
                        "If you did not, someone else may have access to your account. Contact your school office straight away, and change your password."),
                    new EmailBlock.Button("Change your password", appUrl),
                ],
                appUrl));
}
