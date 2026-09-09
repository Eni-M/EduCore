using EduCore.Services.Email;

namespace EduCore.Services.Interfaces;

// The one seam every outgoing email goes through. The transport (SMTP, a provider API, or the
// logging stub) is chosen in DI, so the rest of the app never depends on how mail is delivered.
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, EmailBody body, CancellationToken ct);
}
