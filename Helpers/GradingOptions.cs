using Microsoft.Extensions.Options;

namespace EduCore.Helpers;

// The grading rules of the state the school operates in.
//
// Configuration rather than constants: the 40/60 split is believed right but not certain, and
// changing it must not mean a code change. Validated at startup by the validator below, so a
// bad weighting fails the app on boot instead of quietly skewing every report.
//
// Pure configuration -- values only. Anything derived from these (thresholds, rounding) is a
// rule and lives on GradingCalculator.
public class GradingOptions
{
    public const string SectionName = "Grading";

    // Named for their position, not for what they measure -- what FirstMark and SecondMark
    // represent is the teacher's own decision and is deliberately not recorded.
    public double FirstMarkWeight { get; set; } = 0.40;
    public double SecondMarkWeight { get; set; } = 0.60;

    public int PassMark { get; set; } = 2;
    public int AtRiskMark { get; set; } = 2;
}

// Kept next to the settings it judges, rather than as a fluent chain buried in Program.cs or
// a file of its own: what counts as a valid weighting belongs where the weighting is. Runs at
// startup (ValidateOnStart), so a misconfigured split stops the app rather than quietly
// skewing every statistic and every report.
public class GradingOptionsValidator : IValidateOptions<GradingOptions>
{
    public ValidateOptionsResult Validate(string? name, GradingOptions options)
    {
        var failures = new List<string>();

        if (Math.Abs(options.FirstMarkWeight + options.SecondMarkWeight - 1.0) > 0.0001)
            failures.Add(
                $"FirstMarkWeight ({options.FirstMarkWeight}) and SecondMarkWeight " +
                $"({options.SecondMarkWeight}) must add up to 1.");

        if (options.PassMark is < 1 or > 5)
            failures.Add($"PassMark ({options.PassMark}) must be between 1 and 5.");

        if (options.AtRiskMark is < 1 or > 5)
            failures.Add($"AtRiskMark ({options.AtRiskMark}) must be between 1 and 5.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
