using Microsoft.Extensions.Options;

namespace EduCore.Helpers;

public class AcademicCalendar(IOptions<AcademicCalendarOptions> options) : IAcademicCalendar
{
    private readonly MonthDay _firstPeriodEnd = Parse(options.Value.FirstPeriodEnd);
    private readonly MonthDay _secondPeriodStart = Parse(options.Value.SecondPeriodStart);

    public (DateOnly Start, DateOnly End) FirstPeriod(DateOnly yearStart, DateOnly yearEnd)
    {
        var end = new DateOnly(yearStart.Year, _firstPeriodEnd.Month, _firstPeriodEnd.Day);
        return (yearStart, Clamp(end, yearStart, yearEnd));
    }

    public (DateOnly Start, DateOnly End) SecondPeriod(DateOnly yearStart, DateOnly yearEnd)
    {
        var year = _secondPeriodStart.Month < yearStart.Month ? yearStart.Year + 1 : yearStart.Year;
        var start = new DateOnly(year, _secondPeriodStart.Month, _secondPeriodStart.Day);
        return (Clamp(start, yearStart, yearEnd), yearEnd);
    }

    private static DateOnly Clamp(DateOnly value, DateOnly min, DateOnly max) =>
        value < min ? min : value > max ? max : value;

    private static MonthDay Parse(string value) =>
        MonthDay.TryParse(value, out var result)
            ? result
            : throw new InvalidOperationException($"Invalid AcademicCalendar date '{value}'.");
}
