namespace EduCore.Services.Email;

/// <summary>
/// Why an account email is being sent. Kept as an enum rather than a pre-rendered message so the
/// queue carries facts, not prose -- the wording is decided when the mail is actually composed,
/// and changing it does not require draining anything.
/// </summary>
public enum AccountMailKind
{
    /// <summary>An account was created for this person by their school.</summary>
    Welcome,

    /// <summary>The password changed. Sent for every change, whoever made it.</summary>
    PasswordChanged
}

/// <summary>
/// One queued account email. Carries only what the message needs, copied at the moment of the
/// write -- never the User entity, which belongs to a DbContext that is disposed long before the
/// background sender gets to it.
/// </summary>
public record AccountMailRequest(AccountMailKind Kind, string Email, string FirstName);

/// <summary>
/// Composes account emails. Pure functions of their input, so the wording -- the part most likely
/// to be wrong, and the part a non-engineer will want to change -- is testable without a mail
/// server, a database, or a running application.
///
/// No message here contains a password, a token, or a link carrying either. A welcome mail that
/// shipped credentials would put them in an inbox forever; the reset flow already exists and is
/// single-use and time-limited, so these point people at it instead.
/// </summary>
public static class AccountMailTemplates
{
    /// <summary>
    /// The application's public address, taken from the origin of the password-reset link.
    ///
    /// Derived rather than configured separately, because a second setting for the same fact is a
    /// second setting to forget: the two drift, and the symptom is emails pointing at an address
    /// nobody can reach. One value, already validated as an absolute URL at start-up.
    /// </summary>
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
