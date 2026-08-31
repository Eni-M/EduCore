using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

public class PrivacyOptionsValidator : IValidateOptions<PrivacyOptions>
{
    public ValidateOptionsResult Validate(string? name, PrivacyOptions options) =>
        options.MinimumCellSize < 1
            ? ValidateOptionsResult.Fail(
                $"MinimumCellSize ({options.MinimumCellSize}) must be at least 1. " +
                "A value below 1 would disable cell suppression.")
            : ValidateOptionsResult.Success;
}
