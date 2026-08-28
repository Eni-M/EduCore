using Microsoft.Extensions.Options;

namespace EduCore.Services.Assignment;

// How many subjects a junior-high or high-school teacher may be qualified for at once.
// Elementary is exempt entirely -- a homeroom teacher covers however many subjects the
// school has, since that IS the elementary teaching model.
public class AssignmentOptions
{
    public const string SectionName = "Assignment";
    public int MaxSubjectsPerTeacher { get; set; } = 2;
}

// Kept in the same file as the settings it judges. The rule is one line long and means
// nothing on its own; a reader who wants to know what a valid MaxSubjectsPerTeacher is should
// find the answer where the property is, not one file away.
public class AssignmentOptionsValidator : IValidateOptions<AssignmentOptions>
{
    public ValidateOptionsResult Validate(string? name, AssignmentOptions options) =>
        options.MaxSubjectsPerTeacher < 1
            ? ValidateOptionsResult.Fail(
                $"MaxSubjectsPerTeacher ({options.MaxSubjectsPerTeacher}) must be at least 1.")
            : ValidateOptionsResult.Success;
}
