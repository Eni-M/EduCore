using EduCore.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// An academic year always has exactly two grading periods -- this is what computes their
// default dates when a year is created.
public class AcademicCalendarTests
{
    private static AcademicCalendar Calendar(string firstEnd = "12-26", string secondStart = "01-07") =>
        new(Options.Create(new AcademicCalendarOptions
        {
            FirstPeriodEnd = firstEnd,
            SecondPeriodStart = secondStart
        }));

    // The worked example: a year starting September 1st splits into autumn (ending December)
    // and spring (starting January of the following calendar year).
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

    // The two periods must never overlap, and the second must start after the first ends.
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

    // A year that does not span a calendar boundary the way September-start years do --
    // the computed dates still land, clamped, inside the year's own range.
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
