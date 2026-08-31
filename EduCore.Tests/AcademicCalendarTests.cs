using EduCore.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

public class AcademicCalendarTests
{
    private static AcademicCalendar Calendar(string firstEnd = "12-26", string secondStart = "01-07") =>
        new(Options.Create(new AcademicCalendarOptions
        {
            FirstPeriodEnd = firstEnd,
            SecondPeriodStart = secondStart
        }));

    [Fact]
    public void StandardSeptemberYear_SplitsAcrossTheNewYear()
    {
        var calendar = Calendar();
        var yearStart = new DateOnly(2025, 9, 1);
        var yearEnd = new DateOnly(2026, 6, 30);

        var (firstStart, firstEnd) = calendar.FirstPeriod(yearStart, yearEnd);
        var (secondStart, secondEnd) = calendar.SecondPeriod(yearStart, yearEnd);

        Assert.Equal(yearStart, firstStart);
        Assert.Equal(new DateOnly(2025, 12, 26), firstEnd);
        Assert.Equal(new DateOnly(2026, 1, 7), secondStart);
        Assert.Equal(yearEnd, secondEnd);
    }

    [Fact]
    public void Periods_DoNotOverlap()
    {
        var calendar = Calendar();
        var yearStart = new DateOnly(2025, 9, 1);
        var yearEnd = new DateOnly(2026, 6, 30);

        var (_, firstEnd) = calendar.FirstPeriod(yearStart, yearEnd);
        var (secondStart, _) = calendar.SecondPeriod(yearStart, yearEnd);

        Assert.True(secondStart > firstEnd);
    }

    [Fact]
    public void ShortYear_ClampsBothPeriodsInsideItsOwnRange()
    {
        var calendar = Calendar();
        var yearStart = new DateOnly(2025, 11, 1);
        var yearEnd = new DateOnly(2025, 12, 10);

        var (firstStart, firstEnd) = calendar.FirstPeriod(yearStart, yearEnd);
        var (secondStart, secondEnd) = calendar.SecondPeriod(yearStart, yearEnd);

        Assert.True(firstStart >= yearStart && firstEnd <= yearEnd);
        Assert.True(secondStart >= yearStart && secondEnd <= yearEnd);
    }

    [Theory]
    [InlineData("13-01")]
    [InlineData("12-32")]
    [InlineData("not-a-date")]
    [InlineData("12")]
    public void InvalidMonthDay_FailsToParse(string value) =>
        Assert.False(MonthDay.TryParse(value, out _));

    [Fact]
    public void ValidMonthDay_Parses()
    {
        Assert.True(MonthDay.TryParse("12-26", out var result));
        Assert.Equal(new MonthDay(12, 26), result);
    }
}
