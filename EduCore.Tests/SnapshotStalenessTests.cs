using EduCore.Models;
using EduCore.Services.Statistics;
using Xunit;

namespace EduCore.Tests;

// The rule that decides when stored statistics are recomputed. Getting it wrong is expensive
// in both directions: too eager and every read pays for a full aggregation, too lax and the
// overview screens quietly show last term's numbers.
public class SnapshotStalenessTests
{
    private static StatisticsSnapshotOptions Options() => new()
    {
        ChangeRatio = 0.02,
        MinimumChanges = 5,
        MaximumChanges = 200,
        MaximumAgeMinutes = 720
    };

    private static SchoolStatisticsSnapshot Snapshot(int activeStudents, int changes, DateTime computedAt) =>
        new()
        {
            SchoolId = Guid.NewGuid(),
            StudentsActive = activeStudents,
            ChangesSinceComputed = changes,
            ComputedAt = computedAt
        };

    [Theory]
    // Small schools sit on the floor: 2% of 100 is 2, which would recompute far too often.
    [InlineData(0, 5)]
    [InlineData(100, 5)]
    [InlineData(250, 5)]
    // Above ~250 students the proportional rule takes over.
    [InlineData(1000, 20)]
    [InlineData(5000, 100)]
    // And it stops at the ceiling, so a very large school cannot drift indefinitely.
    [InlineData(10_000, 200)]
    [InlineData(100_000, 200)]
    public void The_threshold_scales_with_the_student_body_between_its_floor_and_ceiling(
        int activeStudents, int expected) =>
        Assert.Equal(expected, SnapshotStaleness.ThresholdFor(activeStudents, Options()));

    [Fact]
    public void A_negative_student_count_still_yields_the_floor_rather_than_a_smaller_threshold()
    {
        // Not reachable from real data, but a threshold below the floor would mean recomputing
        // on every single read, so the clamp must hold regardless of what is in the row.
        Assert.Equal(5, SnapshotStaleness.ThresholdFor(-10, Options()));
    }

    [Fact]
    public void A_snapshot_below_its_threshold_and_within_its_age_is_reused()
    {
        var now = DateTime.UtcNow;
        var snapshot = Snapshot(activeStudents: 1000, changes: 19, computedAt: now.AddMinutes(-10));

        Assert.False(SnapshotStaleness.IsStale(snapshot, Options(), now));
    }

    [Fact]
    public void Reaching_the_threshold_makes_it_stale()
    {
        var now = DateTime.UtcNow;

        // 1,000 students → threshold 20. At exactly 20 it must recompute: the comparison is
        // ">=", so the boundary belongs to the stale side.
        var snapshot = Snapshot(activeStudents: 1000, changes: 20, computedAt: now.AddMinutes(-10));

        Assert.True(SnapshotStaleness.IsStale(snapshot, Options(), now));
    }

    [Fact]
    public void Age_alone_makes_it_stale_even_with_no_writes_at_all()
    {
        var now = DateTime.UtcNow;

        // Nothing was written, but a window that ends "today" covers different days than it did
        // yesterday. Without the age rule those figures would never be recomputed.
        var snapshot = Snapshot(activeStudents: 5000, changes: 0, computedAt: now.AddMinutes(-721));

        Assert.True(SnapshotStaleness.IsStale(snapshot, Options(), now));
    }

    [Fact]
    public void A_snapshot_exactly_at_the_age_limit_is_not_yet_stale()
    {
        var now = DateTime.UtcNow;
        var snapshot = Snapshot(activeStudents: 5000, changes: 0, computedAt: now.AddMinutes(-720));

        Assert.False(SnapshotStaleness.IsStale(snapshot, Options(), now));
    }

    [Fact]
    public void A_small_school_recomputes_after_the_floor_is_reached_not_after_a_proportional_share()
    {
        var now = DateTime.UtcNow;

        // 2% of 50 rounds up to 1. Without the floor, a single write would trigger a full
        // recomputation on every read.
        Assert.False(SnapshotStaleness.IsStale(
            Snapshot(50, changes: 4, computedAt: now), Options(), now));

        Assert.True(SnapshotStaleness.IsStale(
            Snapshot(50, changes: 5, computedAt: now), Options(), now));
    }
}
