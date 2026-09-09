using Microsoft.Extensions.Options;

namespace EduCore.Configuration;

// Limits on what a statistics query may ask for, as opposed to how its answers are cached
// (StatisticsSnapshot) or suppressed (Privacy).
public class StatisticsQueryOptions
{
    public const string SectionName = "Statistics";

    // The widest date range a caller may request. Without a ceiling, one request can scan every
    // attendance and mark row the platform has ever held.
    public int MaximumWindowYears { get; set; } = 10;
}

public class StatisticsQueryOptionsValidator : IValidateOptions<StatisticsQueryOptions>
{
    public ValidateOptionsResult Validate(string? name, StatisticsQueryOptions options) =>
        options.MaximumWindowYears is < 1 or > 50
            ? ValidateOptionsResult.Fail(
                $"Statistics:MaximumWindowYears ({options.MaximumWindowYears}) must be between 1 and 50.")
            : ValidateOptionsResult.Success;
}
