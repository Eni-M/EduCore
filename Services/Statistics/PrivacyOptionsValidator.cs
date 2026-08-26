using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

// Keeps the privacy rule's own validity next to the rule, the same way GradingOptionsValidator
// does for grading. Runs at startup, so a threshold of 0 -- which would disable suppression
// entirely -- stops the app instead of quietly publishing individuals.
public class PrivacyOptionsValidator : IValidateOptions<PrivacyOptions>
{
    public ValidateOptionsResult Validate(string? name, PrivacyOptions options) =>
        options.MinimumCellSize < 1
            ? ValidateOptionsResult.Fail(
                $"MinimumCellSize ({options.MinimumCellSize}) must be at least 1. " +
                "A value below 1 would disable cell suppression.")
            : ValidateOptionsResult.Success;
}
