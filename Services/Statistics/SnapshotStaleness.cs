using EduCore.Models;

namespace EduCore.Services.Statistics;

/// <summary>
/// Decides when a stored snapshot has drifted far enough from the live data to be worth
/// recomputing. Pure functions of the row, the options and the clock, so the policy can be
/// reasoned about and tested without a database.
/// </summary>
public static class SnapshotStaleness
{
    /// <summary>
    /// How many contributing writes a school of this size may absorb before its figures are
    /// recomputed.
    /// </summary>
    /// <remarks>
    /// Proportional to the student body rather than fixed. A fixed threshold is wrong at both
    /// ends: 20 writes is most of a 50-student school changing, and a rounding error in one of
    /// 5,000. Clamped so a tiny school does not recompute on every write and a very large one
    /// cannot drift indefinitely.
    /// </remarks>
    public static int ThresholdFor(int activeStudents, StatisticsSnapshotOptions options) =>
        Math.Clamp(
            (int)Math.Ceiling(Math.Max(activeStudents, 0) * options.ChangeRatio),
            options.MinimumChanges,
            options.MaximumChanges);

    /// <summary>
    /// Stale on either count: enough contributing writes to move the figures, or enough time
    /// that a window ending "today" no longer covers the same days it did when it was computed.
    /// </summary>
    public static bool IsStale(
        SchoolStatisticsSnapshot snapshot, StatisticsSnapshotOptions options, DateTime nowUtc) =>
        snapshot.ChangesSinceComputed >= ThresholdFor(snapshot.StudentsActive, options)
        || nowUtc - snapshot.ComputedAt > TimeSpan.FromMinutes(options.MaximumAgeMinutes);
}
