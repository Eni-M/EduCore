using EduCore.Models;

namespace EduCore.Services.Statistics;

// When a stored snapshot has drifted far enough from live data to be worth recomputing.
public static class SnapshotStaleness
{
    // Scaled to the student body: 20 writes is most of a 50-student school and a rounding error
    // in one of 5,000. Clamped at both ends.
    public static int ThresholdFor(int activeStudents, StatisticsSnapshotOptions options) =>
        Math.Clamp(
            (int)Math.Ceiling(Math.Max(activeStudents, 0) * options.ChangeRatio),
            options.MinimumChanges,
            options.MaximumChanges);

    // Stale on either count: enough writes to move the figures, or old enough that a window
    // ending "today" no longer covers the same days.
    public static bool IsStale(
        SchoolStatisticsSnapshot snapshot, StatisticsSnapshotOptions options, DateTime nowUtc) =>
        snapshot.ChangesSinceComputed >= ThresholdFor(snapshot.StudentsActive, options)
        || nowUtc - snapshot.ComputedAt > TimeSpan.FromMinutes(options.MaximumAgeMinutes);
}
