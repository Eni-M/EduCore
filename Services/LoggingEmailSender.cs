using Microsoft.Extensions.Logging;

namespace EduCore.Services.Email;

// Default transport until a real one (SMTP / provider API) is wired in: it logs what would be
// sent. Swap this registration in Program.cs for the real sender without touching any caller.
// stub transport, replace with SMTP/provider when the client picks one.
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
