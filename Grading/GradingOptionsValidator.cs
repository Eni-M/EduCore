using Microsoft.Extensions.Options;

namespace EduCore.Grading;

// Keeps the grading rules' own validity next to the grading rules, instead of as a fluent
// chain buried in Program.cs. Runs at startup (ValidateOnStart), so a misconfigured weighting
// stops the app rather than quietly skewing every statistic and every report.
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
