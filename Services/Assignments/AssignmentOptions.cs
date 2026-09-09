using Microsoft.Extensions.Options;

namespace EduCore.Services.Assignments;

public class AssignmentOptions
{
    public const string SectionName = "Assignment";
    public int MaxSubjectsPerTeacher { get; set; } = 2;
}

public class AssignmentOptionsValidator : IValidateOptions<AssignmentOptions>
{
    public ValidateOptionsResult Validate(string? name, AssignmentOptions options) =>
        options.MaxSubjectsPerTeacher < 1
            ? ValidateOptionsResult.Fail(
                $"MaxSubjectsPerTeacher ({options.MaxSubjectsPerTeacher}) must be at least 1.")
            : ValidateOptionsResult.Success;
}
