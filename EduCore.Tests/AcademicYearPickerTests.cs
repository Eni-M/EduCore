using EduCore.Helpers;
using EduCore.Models;
using Xunit;

namespace EduCore.Tests;

public class AcademicYearPickerTests
{
    private static AcademicYear Year(string start, string end) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = $"{start} to {end}",
            StartDate = DateOnly.Parse(start),
            EndDate = DateOnly.Parse(end)
        };

    private static readonly AcademicYear Finished = Year("2025-09-01", "2026-06-30");
    private static readonly AcademicYear Upcoming = Year("2026-09-01", "2027-06-30");
    private static readonly AcademicYear LongPast = Year("2023-08-01", "2024-06-30");

    [Fact]
    public void InTheSummerGap_PicksTheYearThatJustEnded_NotTheOneAboutToStart()
    {
        var picked = AcademicYearPicker.CurrentOn(
            [LongPast, Finished, Upcoming], new DateOnly(2026, 8, 28));

        Assert.Equal(Finished.Id, picked?.Id);
    }

    [Fact]
    public void DuringTerm_PicksTheYearTodayFallsInside()
    {
        var picked = AcademicYearPicker.CurrentOn(
            [LongPast, Finished, Upcoming], new DateOnly(2025, 11, 20));

        Assert.Equal(Finished.Id, picked?.Id);
    }

    [Fact]
    public void WithSeveralFinishedYears_PicksTheMostRecentlyEnded()
    {
        var picked = AcademicYearPicker.CurrentOn(
            [LongPast, Finished], new DateOnly(2026, 8, 28));

        Assert.Equal(Finished.Id, picked?.Id);
    }

    [Fact]
    public void WithNothingFinishedYet_PicksTheSoonestYearToCome()
    {
        var later = Year("2027-09-01", "2028-06-30");

        var picked = AcademicYearPicker.CurrentOn(
            [later, Upcoming], new DateOnly(2026, 8, 28));

        Assert.Equal(Upcoming.Id, picked?.Id);
    }

    [Fact]
    public void WithNoYearsAtAll_PicksNothing() =>
        Assert.Null(AcademicYearPicker.CurrentOn([], new DateOnly(2026, 8, 28)));

    [Theory]
    [InlineData("2025-09-01")]
    [InlineData("2026-06-30")]
    public void TheFirstAndLastDayOfAYear_CountAsInsideIt(string day)
    {
        var picked = AcademicYearPicker.CurrentOn([Finished, Upcoming], DateOnly.Parse(day));

        Assert.Equal(Finished.Id, picked?.Id);
    }
}
