namespace EduCore.Services.Email;

// An enum rather than pre-rendered text, so the queue carries facts and the wording is decided
// at composition time.
public enum AccountMailKind
{
    Welcome,

    // Sent for every change, whoever made it.
    PasswordChanged
}

// Copied at the moment of the write, never the User entity -- its DbContext is long disposed by
// the time the background sender runs.
public record AccountMailRequest(AccountMailKind Kind, string Email, string FirstName);

// Composes account emails as pure functions, so the wording is testable without a mail server.
// No message carries a password, a token, or a link holding either -- they point at the reset
// flow, which is single-use and time-limited.
public static class AccountMailTemplates
{
    // Derived from the reset link's origin rather than configured twice: two settings for one
    // fact drift, and the symptom is mail pointing somewhere nobody can reach.
    public static string AppUrlFrom(string resetUrlTemplate) =>
        Uri.TryCreate(resetUrlTemplate, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : resetUrlTemplate;

    public static (string Subject, string Body) Compose(AccountMailRequest request, string appUrl) =>
        request.Kind switch
        {
            AccountMailKind.Welcome => Welcome(request, appUrl),
            AccountMailKind.PasswordChanged => PasswordChanged(request, appUrl),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

    private static (string, string) Welcome(AccountMailRequest request, string appUrl) =>
        ("Your EduCore account is ready",
            $"""
             Hello {request.FirstName},

             Your school has created an EduCore account for you. You sign in with this email
             address:

             {request.Email}

             Your school will give you a temporary password. You will be asked to choose your own
             the first time you sign in.

             {appUrl}

             If nobody has given you a password, use the "Forgot your password?" link on the
             sign-in page and we will email you one that works once.
             """);

    private static (string, string) PasswordChanged(AccountMailRequest request, string appUrl) =>
        ("Your EduCore password was changed",
            $"""
             Hello {request.FirstName},

             The password for your EduCore account ({request.Email}) has just been changed.

             If you made this change, there is nothing to do.

             If you did not, someone else may have access to your account. Contact your
             school office straight away, and change your password here:

             {appUrl}
             """);
}
