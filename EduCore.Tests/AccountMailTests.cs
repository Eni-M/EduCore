using EduCore.Services.Email;
using Xunit;

namespace EduCore.Tests;

// Account emails leave the building and cannot be recalled, so the properties worth pinning are
// the ones that would be embarrassing or dangerous to get wrong: no secrets in the body, the
// right address quoted back, and a link that points somewhere the recipient can actually reach.
public class AccountMailTests
{
    private const string AppUrl = "https://edu-core-front.vercel.app";

    private static AccountMailRequest Request(AccountMailKind kind) =>
        new(kind, "ana.berisha@example.org", "Ana");

    // Both parts, concatenated: every rule below has to hold for the HTML as well as the text,
    // and a secret that leaked into only one of them would otherwise pass.
    private static (string Subject, string Body) Compose(AccountMailKind kind)
    {
        var (subject, body) = AccountMailTemplates.Compose(Request(kind), AppUrl);
        return (subject, body.Text + "\n" + body.Html);
    }

    private static EmailBody Rendered(AccountMailKind kind) =>
        AccountMailTemplates.Compose(Request(kind), AppUrl).Body;

    // ---- what must never appear -------------------------------------------------------

    [Theory]
    [InlineData(AccountMailKind.Welcome)]
    [InlineData(AccountMailKind.PasswordChanged)]
    public void No_account_email_contains_a_password_or_a_token(AccountMailKind kind)
    {
        // The reset flow exists precisely so credentials do not have to travel by email, where
        // they sit in an inbox indefinitely. Nothing here should carry one.
        var (subject, body) = Compose(kind);
        var text = $"{subject} {body}".ToLowerInvariant();

        Assert.DoesNotContain("token=", text);
        Assert.DoesNotContain("temporary password is", text);
        Assert.DoesNotContain("your password is", text);
    }

    [Theory]
    [InlineData(AccountMailKind.Welcome)]
    [InlineData(AccountMailKind.PasswordChanged)]
    public void Every_account_email_is_addressed_and_signposted(AccountMailKind kind)
    {
        var (subject, body) = Compose(kind);

        Assert.NotEmpty(subject);
        Assert.Contains("Ana", body);
        Assert.Contains(AppUrl, body);
    }

    // ---- welcome -----------------------------------------------------------------------

    [Fact]
    public void The_welcome_email_quotes_the_address_used_to_sign_in()
    {
        // Sign-in is by email, and a school creating the account may have used a different
        // address from the one the person expects. Saying which one avoids the support call.
        var (subject, body) = Compose(AccountMailKind.Welcome);

        Assert.Contains("ana.berisha@example.org", body);
        Assert.Contains("account", subject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_welcome_email_explains_what_to_do_with_no_password()
    {
        // The likeliest failure of an account handover is that nobody passes the password on.
        // The recovery path is the reset flow, so the mail names it.
        var (_, body) = Compose(AccountMailKind.Welcome);

        Assert.Contains("Forgot your password?", body);
    }

    // ---- password changed ---------------------------------------------------------------

    [Fact]
    public void The_password_change_email_tells_the_reader_what_to_do_if_it_was_not_them()
    {
        // The entire point of this message. A change notice with no call to action is noise;
        // this one is the only warning a compromised account gets.
        var (subject, body) = Compose(AccountMailKind.PasswordChanged);

        Assert.Contains("changed", subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("did not", body);
        Assert.Contains("school office", body);
    }

    // ---- the application address ---------------------------------------------------------

    [Theory]
    [InlineData("https://edu-core-front.vercel.app/reset-password", "https://edu-core-front.vercel.app")]
    [InlineData("http://localhost:5173/reset-password", "http://localhost:5173")]
    [InlineData("https://localhost:8443/reset-password", "https://localhost:8443")]
    [InlineData("https://school.example.org/auth/reset", "https://school.example.org")]
    public void The_application_address_is_the_origin_of_the_reset_link(string template, string expected)
    {
        // Derived from the reset template rather than configured separately: two settings for the
        // same fact drift, and the symptom is emails pointing somewhere nobody can reach.
        Assert.Equal(expected, AccountMailTemplates.AppUrlFrom(template));
    }

    [Fact]
    public void A_malformed_reset_template_is_passed_through_rather_than_crashing_the_sender()
    {
        // Start-up validation already rejects a non-absolute template, so this is unreachable in
        // practice. It matters anyway: the sender is a background service, and an exception here
        // would stop every later message, not just this one.
        Assert.Equal("not-a-url", AccountMailTemplates.AppUrlFrom("not-a-url"));
    }
}
