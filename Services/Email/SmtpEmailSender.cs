using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Email;

// Delivers over SMTP, which is the one transport every provider speaks -- an on-premises relay,
// Gmail, Brevo, SendGrid and the rest all accept these same settings, so switching provider is a
// configuration change and not a code change.
//
// A connection per send, deliberately. MailKit's SmtpClient is not thread-safe and holds server
// state across a session, so sharing one across the digest's five concurrent sends would corrupt
// it. Connection setup costs a TLS handshake; at this application's mail volume -- password
// resets and one daily digest -- that is cheaper than the failure modes of a shared client.
public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, EmailBody body, CancellationToken ct)
    {
        if (!MailboxAddress.TryParse(toEmail, out var recipient))
            throw new InvalidOperationException(
                $"'{toEmail}' is not a valid email address, so no message could be addressed to it.");

        // multipart/alternative: the client picks the richest part it can render, and the ones
        // that refuse HTML -- or a spam filter reading the message -- still get real text.
        var message = new MimeMessage
        {
            Subject = subject,
            Body = new BodyBuilder { TextBody = body.Text, HtmlBody = body.Html }.ToMessageBody()
        };

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(recipient);

        using var client = new SmtpClient { Timeout = _options.TimeoutSeconds * 1000 };

        try
        {
            // StartTls, not StartTlsWhenAvailable: "when available" quietly falls back to an
            // unencrypted session against a server that does not advertise the extension, which
            // would put password-reset links on the wire in clear text. Fail instead.
            var security = _options.UseSslOnConnect
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_options.Host, _options.Port, security, ct);

            // An empty username means a relay that authorizes by IP address; authenticating
            // against one that expects no credentials is itself an error.
            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Logged here because this is the only place that knows which recipient and which
            // host failed; rethrown because the caller decides whether a failed send is fatal
            // (a password reset) or one skippable recipient among many (the digest).
            logger.LogError(ex,
                "SMTP send to {Recipient} via {Host}:{Port} failed.",
                toEmail, _options.Host, _options.Port);

            throw;
        }
    }
}
