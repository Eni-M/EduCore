using Microsoft.Extensions.Options;

namespace EduCore.Helpers;

public class AcademicCalendarOptions
{
    public const string SectionName = "AcademicCalendar";

    public string FirstPeriodEnd { get; set; } = "12-26";

    public string SecondPeriodStart { get; set; } = "01-07";
}

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
