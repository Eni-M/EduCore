using Microsoft.Extensions.Options;

namespace EduCore.Helpers;

// The default split of an academic year into its two grading periods. An academic year
// always has exactly two -- there is no add/remove; only the dates are ever editable, and
// these are only the starting point for a newly created year.
public class AcademicCalendarOptions
{
    public const string SectionName = "AcademicCalendar";

    // MM-dd. Assumed to fall in the same calendar year the academic year starts.
    public string FirstPeriodEnd { get; set; } = "12-26";

    // MM-dd. Assumed to fall in the calendar year after the one the academic year starts,
    // since a school year that begins in autumn resumes in January.
    public string SecondPeriodStart { get; set; } = "01-07";
}

// A month and day with no year -- what the two settings above hold, since the same MM-dd
// default applies regardless of which calendar year an academic year happens to start in.
// It lives here rather than in a file of its own because it exists to describe them: what a
// valid FirstPeriodEnd is, and what parsing one means, is a single idea.
public readonly record struct MonthDay(int Month, int Day)
{
    public static bool TryParse(string value, out MonthDay result)
    {
        result = default;
        var parts = value.Split('-');

        if (parts.Length != 2
            || !int.TryParse(parts[0], out var month) || month is < 1 or > 12
            || !int.TryParse(parts[1], out var day) || day is < 1 or > 31)
            return false;

        result = new MonthDay(month, day);
        return true;
    }
}

// The startup check, so a malformed MM-dd stops the app rather than throwing on the first
// academic year somebody creates.
public class AcademicCalendarOptionsValidator : IValidateOptions<AcademicCalendarOptions>
{
    public ValidateOptionsResult Validate(string? name, AcademicCalendarOptions options)
    {
        if (!MonthDay.TryParse(options.FirstPeriodEnd, out _))
            return ValidateOptionsResult.Fail(
                $"AcademicCalendar:FirstPeriodEnd ('{options.FirstPeriodEnd}') must be in MM-dd format.");

        if (!MonthDay.TryParse(options.SecondPeriodStart, out _))
            return ValidateOptionsResult.Fail(
                $"AcademicCalendar:SecondPeriodStart ('{options.SecondPeriodStart}') must be in MM-dd format.");

        return ValidateOptionsResult.Success;
    }
}
