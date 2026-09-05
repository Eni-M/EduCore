using EduCore.Services.Email;
using Xunit;

namespace EduCore.Tests;

/// <summary>
/// Mail configuration fails in the worst possible way: silently, in a background job, hours
/// after start-up, to a recipient who simply never receives their password-reset link. The
/// validator exists to turn every one of those into a start-up failure, so it is worth testing
/// as carefully as the feature itself.
/// </summary>
public class EmailOptionsTests
{
    private static EmailOptions ValidSmtp() => new()
    {
        Provider = "Smtp",
        Host = "smtp.example.org",
        Port = 587,
        FromAddress = "noreply@educore.example",
        FromName = "EduCore",
        Username = "apikey",
        Password = "secret",
        TimeoutSeconds = 30
    };

    private static (bool Succeeded, string Failures) Validate(EmailOptions options)
    {
        var result = new EmailOptionsValidator().Validate(null, options);

        return (result.Succeeded, string.Join(" | ", result.Failures ?? []));
    }

    [Fact]
    public void Default_configuration_is_the_logging_transport()
    {
        var options = new EmailOptions();

        Assert.False(options.IsSmtp);
        Assert.True(Validate(options).Succeeded);
    }

    [Theory]
    [InlineData("Smtp")]
    [InlineData("smtp")]
    [InlineData("SMTP")]
    public void Provider_matching_ignores_case(string provider)
    {
        Assert.True((new EmailOptions { Provider = provider }).IsSmtp);
    }

    [Fact]
    public void Unknown_provider_is_rejected()
    {
        var (succeeded, failures) = Validate(new EmailOptions { Provider = "Sendgrid" });

        Assert.False(succeeded);
        Assert.Contains("Email:Provider", failures);
    }

    [Fact]
    public void Log_transport_ignores_every_smtp_setting()
    {
        // Nothing else is required while mail is not being delivered, so a half-filled SMTP
        // block must not stop the application from starting in development.
        var options = new EmailOptions { Provider = "Log", Host = "", FromAddress = "", Port = 0 };

        Assert.True(Validate(options).Succeeded);
    }

    [Fact]
    public void Smtp_transport_accepts_a_complete_configuration()
    {
        Assert.True(Validate(ValidSmtp()).Succeeded);
    }

    [Fact]
    public void Smtp_transport_requires_a_host()
    {
        var options = ValidSmtp();
        options.Host = "";

        var (succeeded, failures) = Validate(options);

        Assert.False(succeeded);
        Assert.Contains("Email:Host", failures);
    }

    [Fact]
    public void Smtp_transport_requires_a_from_address()
    {
        var options = ValidSmtp();
        options.FromAddress = "";

        var (succeeded, failures) = Validate(options);

        Assert.False(succeeded);
        Assert.Contains("Email:FromAddress", failures);
    }

    [Fact]
    public void A_malformed_from_address_is_rejected()
    {
        var options = ValidSmtp();
        options.FromAddress = "not-an-address";

        var (succeeded, failures) = Validate(options);

        Assert.False(succeeded);
        Assert.Contains("valid email address", failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void A_port_outside_the_valid_range_is_rejected(int port)
    {
        var options = ValidSmtp();
        options.Port = port;

        var (succeeded, failures) = Validate(options);

        Assert.False(succeeded);
        Assert.Contains("Email:Port", failures);
    }

    [Fact]
    public void A_relay_that_authorizes_by_ip_needs_no_credentials()
    {
        var options = ValidSmtp();
        options.Username = "";
        options.Password = "";

        Assert.True(Validate(options).Succeeded);
    }

    [Theory]
    [InlineData("apikey", "")]
    [InlineData("", "secret")]
    public void Half_a_credential_pair_is_rejected(string username, string password)
    {
        // The failure this catches is authenticating with an empty password, which a server
        // rejects at the first send -- long after start-up, inside a background job.
        var options = ValidSmtp();
        options.Username = username;
        options.Password = password;

        var (succeeded, failures) = Validate(options);

        Assert.False(succeeded);
        Assert.Contains("must be set together", failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public void An_unreasonable_timeout_is_rejected(int seconds)
    {
        var options = ValidSmtp();
        options.TimeoutSeconds = seconds;

        var (succeeded, failures) = Validate(options);

        Assert.False(succeeded);
        Assert.Contains("Email:TimeoutSeconds", failures);
    }

    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        // One start-up, one list of everything wrong: fixing configuration one failure per
        // restart is the reason misconfiguration takes an afternoon.
        var options = new EmailOptions { Provider = "Smtp", Port = 0, TimeoutSeconds = 0 };

        var (succeeded, failures) = Validate(options);

        Assert.False(succeeded);
        Assert.Contains("Email:Host", failures);
        Assert.Contains("Email:Port", failures);
        Assert.Contains("Email:FromAddress", failures);
        Assert.Contains("Email:TimeoutSeconds", failures);
    }
}
