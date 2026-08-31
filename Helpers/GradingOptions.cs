using Microsoft.Extensions.Options;

namespace EduCore.Helpers;

public class GradingOptions
{
    public const string SectionName = "Grading";

    public double FirstMarkWeight { get; set; } = 0.40;
    public double SecondMarkWeight { get; set; } = 0.60;

    public int PassMark { get; set; } = 2;
    public int AtRiskMark { get; set; } = 2;
}

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
