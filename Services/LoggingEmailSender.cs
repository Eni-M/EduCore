using Microsoft.Extensions.Logging;

namespace EduCore.Services.Email;

// Default transport until a real one (SMTP / provider API) is wired in: it logs what would be
// sent. Swap this registration in Program.cs for the real sender without touching any caller.
// stub transport, replace with SMTP/provider when the client picks one.
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, EmailBody body, CancellationToken ct)
    {
        // The text part, not the HTML: a log line full of table markup hides the thing you
        // opened the log to read.
        logger.LogInformation("Email to {To}: {Subject}{NewLine}{Body}",
            toEmail, subject, Environment.NewLine, body.Text);

        return Task.CompletedTask;
    }
}
