using Microsoft.Extensions.Options;

namespace EduCore.Services.Calendar;

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
