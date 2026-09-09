using Microsoft.Extensions.Options;

namespace EduCore.Services.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    // Bounded so a real transport is never hit with unlimited parallel sends when a digest run
    // fans out to a whole school.
    public int MaxConcurrentSends { get; set; } = 5;

    // The hand-off between the write that causes account mail and the background sender. Bounded
    // and drop-oldest, so a stuck sender cannot grow into a crash; losing a welcome email is a
    // better outcome than losing the process.
    public int QueueCapacity { get; set; } = 500;

    // "Log" writes what would be sent to the application log and delivers nothing -- the right
    // default for development and for a machine with no mail credentials. "Smtp" delivers over
    // SMTP using the settings below. Chosen in Program.cs; see StorageOptions for the same shape.
    public string Provider { get; set; } = "Log";

    // Whether this transport actually delivers mail. Password reset and the schedule digests are
    // silently useless when it does not, so start-up says so out loud rather than leaving it to
    // be discovered by a user who never receives their reset link.
    public bool IsSmtp => string.Equals(Provider, "Smtp", StringComparison.OrdinalIgnoreCase);

    public string Host { get; set; } = "";

    // 587 (STARTTLS) is the modern submission port and what almost every provider wants. 465 is
    // implicit TLS, still common; 25 is server-to-server relay and is blocked by most networks.
    public int Port { get; set; } = 587;

    // The address recipients see, and the one the provider authorizes you to send as. Providers
    // reject or spam-file mail whose From does not match the authenticated account.
    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "EduCore";

    // Left empty for a relay that authorizes by IP (common for an on-premises mail server).
    // Anything public needs both. These are secrets: set them in .env, never in appsettings.
    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    // Implicit TLS from the first byte (port 465). Leave false for 587, where the connection
    // starts in the clear and is upgraded by STARTTLS -- MailKit requires that upgrade and will
    // fail rather than continue unencrypted, so false does not mean "unencrypted".
    public bool UseSslOnConnect { get; set; } = false;

    // A mail server that has gone away must not hold an HTTP request open indefinitely. Applies
    // to connect, authenticate and send separately.
    public int TimeoutSeconds { get; set; } = 30;
}

public class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    private static readonly string[] KnownProviders = ["Log", "Smtp"];

    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (!KnownProviders.Contains(options.Provider, StringComparer.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail(
                $"Email:Provider ('{options.Provider}') must be one of: {string.Join(", ", KnownProviders)}.");

        // Every other setting only exists to serve the SMTP transport.
        if (!options.IsSmtp)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
            failures.Add("Email:Host must be set when Email:Provider is Smtp.");

        if (options.Port is < 1 or > 65535)
            failures.Add($"Email:Port ({options.Port}) must be between 1 and 65535.");

        if (string.IsNullOrWhiteSpace(options.FromAddress))
            failures.Add("Email:FromAddress must be set when Email:Provider is Smtp.");
        else if (!IsDeliverableAddress(options.FromAddress))
            failures.Add($"Email:FromAddress ('{options.FromAddress}') is not a valid email address.");

        // A username without a password is a misconfiguration that fails at the first send,
        // in a background job, hours later. Catch it at start-up instead.
        if (string.IsNullOrWhiteSpace(options.Username) != string.IsNullOrWhiteSpace(options.Password))
            failures.Add(
                "Email:Username and Email:Password must be set together -- supply both for an "
                + "authenticated relay, or neither for one that authorizes by IP address.");

        if (options.TimeoutSeconds is < 1 or > 300)
            failures.Add($"Email:TimeoutSeconds ({options.TimeoutSeconds}) must be between 1 and 300.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    // MimeKit parses a bare local part such as "noreply" as a valid mailbox -- correct for the
    // RFC, useless for delivery. Require a domain too, since this address is what every provider
    // authenticates and every recipient replies to.
    private static bool IsDeliverableAddress(string value) =>
        MimeKit.MailboxAddress.TryParse(value, out var mailbox)
        && mailbox.Address.Split('@') is [{ Length: > 0 }, { Length: > 0 } domain]
        && domain.Contains('.');
}
