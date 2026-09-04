namespace EduCore.Services.Email;

// Builds and sends the scheduled digests. Runs from a background scope, so it queries across all
// schools (the per-request school filter is bypassed) rather than for one signed-in user.
public interface IScheduleDigestService
{
    // Tomorrow's lessons for teachers and students, plus tomorrow's meetings for directorates.
    Task SendDailyAsync(DateOnly today, CancellationToken ct);

    // The coming week's lessons for teachers and students. Sent on Sundays.
    Task SendWeeklyAsync(DateOnly today, CancellationToken ct);
}
