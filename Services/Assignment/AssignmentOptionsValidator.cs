using Microsoft.Extensions.Options;

namespace EduCore.Services.Assignment;

public class AssignmentOptionsValidator : IValidateOptions<AssignmentOptions>
{
    public ValidateOptionsResult Validate(string? name, AssignmentOptions options) =>
        options.MaxSubjectsPerTeacher < 1
            ? ValidateOptionsResult.Fail(
                $"MaxSubjectsPerTeacher ({options.MaxSubjectsPerTeacher}) must be at least 1.")
            : ValidateOptionsResult.Success;
}
