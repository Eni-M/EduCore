using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

public class StatisticsSnapshotOptions
{
    public const string SectionName = "StatisticsSnapshot";

    // Whether overview screens read snapshots at all. Turning this off falls back to computing
    // every figure live, which is the behaviour that existed before the table.
    public bool Enabled { get; set; } = true;

    // How stale a snapshot may get, as a share of the school's student body. At 0.02 a school
    // of 1,000 students tolerates 20 contributing writes before recomputing, one of 5,000
    // tolerates 100 — bigger schools change more in absolute terms without the aggregate
    // figures moving, so they earn a proportionally longer reuse.
    public double ChangeRatio { get; set; } = 0.02;

    // Floor and ceiling on the derived threshold. The floor stops a tiny school recomputing on
    // literally every write; the ceiling stops a very large one from drifting for too long.
    public int MinimumChanges { get; set; } = 5;
    public int MaximumChanges { get; set; } = 200;

    // A snapshot older than this is recomputed no matter how few writes there were. Windows
    // that end "today" move on their own as time passes, with no write to notice it.
    public int MaximumAgeMinutes { get; set; } = 720;

    // How many stale schools one cross-school request may recompute. Each refresh is about 16
    // round trips, so refreshing a whole page of 80 stale schools in one request was ~1,300
    // queries. Past this many, the rest are served from their existing snapshot, a little out of
    // date, and catch up on later requests. A school with no snapshot yet is always computed.
    public int MaximumRefreshesPerRequest { get; set; } = 10;
}

public class StatisticsSnapshotOptionsValidator : IValidateOptions<StatisticsSnapshotOptions>
{
    public ValidateOptionsResult Validate(string? name, StatisticsSnapshotOptions options)
    {
        var failures = new List<string>();

        if (options.MaximumRefreshesPerRequest < 1)
            failures.Add(
                $"MaximumRefreshesPerRequest ({options.MaximumRefreshesPerRequest}) must be at least 1.");

        if (options.ChangeRatio is < 0 or > 1)
            failures.Add($"ChangeRatio ({options.ChangeRatio}) must be between 0 and 1.");

        if (options.MinimumChanges < 1)
            failures.Add(
                $"MinimumChanges ({options.MinimumChanges}) must be at least 1. " +
                "A threshold of 0 would recompute on every read.");

        if (options.MaximumChanges < options.MinimumChanges)
            failures.Add(
                $"MaximumChanges ({options.MaximumChanges}) must not be below " +
                $"MinimumChanges ({options.MinimumChanges}).");

        if (options.MaximumAgeMinutes < 1)
            failures.Add($"MaximumAgeMinutes ({options.MaximumAgeMinutes}) must be at least 1.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
