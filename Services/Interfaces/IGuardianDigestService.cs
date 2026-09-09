namespace EduCore.Services.Interfaces;

public interface IGuardianDigestService
{
    // One email per guardian, covering the seven days ending on today.
    // Returns how many were sent, so the caller can log a run that quietly did nothing.
    Task<int> SendWeeklyAsync(DateOnly today, CancellationToken ct);
}
