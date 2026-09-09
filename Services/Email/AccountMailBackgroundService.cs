using EduCore.Services.Account;
using Microsoft.Extensions.Options;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Email;

// Drains the account mail queue and sends what it finds.
//
// A singleton reading a queue, resolving a scope per message, because IEmailSender is scoped.
// One message at a time on purpose: these arrive at human pace -- an account created, a password
// changed -- and a mail server would rather have a trickle than a burst.
//
// A send that fails is logged and dropped, never retried and never rethrown. A welcome email is
// not worth taking down a background service for, and the sender already logs which recipient
// and which host failed. Anything that must be delivered needs a durable outbox with retries,
// which this deliberately is not.
public class AccountMailBackgroundService(
    IAccountMailQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<PasswordResetOptions> passwordReset,
    ILogger<AccountMailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var appUrl = AccountMailTemplates.AppUrlFrom(passwordReset.Value.ResetUrlTemplate);

        await foreach (var request in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                var (subject, body) = AccountMailTemplates.Compose(request, appUrl);

                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                await sender.SendAsync(request.Email, subject, body, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send a {Kind} account email.", request.Kind);
            }
        }
    }
}
