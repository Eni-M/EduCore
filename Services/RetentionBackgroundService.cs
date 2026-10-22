using EduCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduCore.Services;

public class RetentionOptions
{
    public const string SectionName = "Retention";

    public bool Enabled { get; set; } = true;

    // Long enough to investigate an incident reported late, short enough that the audit table --
    // which names who read which child's records -- is not kept forever. Set it to whatever the
    // DPIA and the school authority decide; 0 keeps everything.
    public int AuditLogDays { get; set; } = 730;

    // Used or expired sign-in codes and reset links are dead after minutes; a month is ample for
    // tracing a complaint about one.
    public int SpentTokenDays { get; set; } = 30;
}

// Deletes what has outlived its purpose, once a day: old audit rows and spent sign-in codes and
// reset links. Student records are not touched -- how long a school keeps a pupil's file after
// they leave is a legal retention decision for the DPIA, not a default in code.
public class RetentionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<RetentionOptions> options,
    ILogger<RetentionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Retention pass failed; it runs again tomorrow.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var audit = 0;
        if (options.Value.AuditLogDays > 0)
        {
            var cutoff = now.AddDays(-options.Value.AuditLogDays);
            audit = await db.AuditLogs.Where(a => a.Timestamp < cutoff).ExecuteDeleteAsync(ct);
        }

        var tokenCutoff = now.AddDays(-options.Value.SpentTokenDays);
        var codes = await db.TwoFactorChallenges.Where(c => c.ExpiresAt < tokenCutoff).ExecuteDeleteAsync(ct);
        var links = await db.PasswordResetTokens.Where(t => t.ExpiresAt < tokenCutoff).ExecuteDeleteAsync(ct);

        logger.LogInformation(
            "Retention pass removed {Audit} audit rows, {Codes} sign-in codes and {Links} reset links.",
            audit, codes, links);
    }
}
