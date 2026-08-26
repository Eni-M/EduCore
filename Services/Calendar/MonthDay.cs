namespace EduCore.Services.Calendar;

// A month and day with no year -- what AcademicCalendarOptions stores, since the same MM-dd
// default applies regardless of which calendar year an academic year happens to start in.
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
