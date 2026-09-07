using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EduCore.Services.Email;

// Once a day at a configured UTC hour: send tomorrow's schedule (and meeting reminders); on
// Sundays also send the week ahead. The digest work runs in a fresh DI scope because this service
// is a singleton and the digest service (and DbContext) are scoped.
public class ScheduledEmailBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<ScheduledEmailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("ScheduledEmail:Enabled", true))
        {
            logger.LogInformation("Scheduled email digests are disabled.");
            return;
        }

        var runHour = config.GetValue("ScheduledEmail:RunHourUtc", 17);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun(DateTime.UtcNow, runHour);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var digests = scope.ServiceProvider.GetRequiredService<IScheduleDigestService>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            await digests.SendDailyAsync(today, ct);

            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                await digests.SendWeeklyAsync(today, ct);

                // Guardians get one summary a week rather than a mail per mark or absence. A
                // parent of three would otherwise receive dozens, and the reliable result of that
                // is a filter rule hiding all of them -- including the one that mattered.
                var guardianDigests = scope.ServiceProvider.GetRequiredService<IGuardianDigestService>();
                var sent = await guardianDigests.SendWeeklyAsync(today, ct);

                logger.LogInformation("Sent {Count} guardian weekly digests.", sent);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A digest run failing must not kill the loop -- log and try again tomorrow.
            logger.LogError(ex, "Scheduled email digest run failed.");
        }
    }

    // The next occurrence of runHour:00 UTC, at least a moment in the future.
    private static TimeSpan TimeUntilNextRun(DateTime nowUtc, int runHour)
    {
        var next = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, runHour, 0, 0, DateTimeKind.Utc);
        if (next <= nowUtc) next = next.AddDays(1);
        return next - nowUtc;
    }
}
