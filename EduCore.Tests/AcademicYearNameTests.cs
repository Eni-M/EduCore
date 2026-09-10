using EduCore.Dtos.AcademicYear;
using EduCore.Helpers;
using EduCore.Validation.AcademicYear;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The dev database holds a year named "2026/2027" that actually ran 2023-01-01 to 2026-09-09.
// Its second term ended the day before a teacher tried to record a mark, and all they were
// told was that today fell outside every term. These are the rules that make that unwritable.
public class AcademicYearNameTests
{
    private static AcademicYearCreateDtoValidator Validator(int back = 1, int forward = 1) =>
        new(Options.Create(new AcademicCalendarOptions
        {
            EarliestStartYearsAgo = back,
            LatestStartYearsAhead = forward
        }));

    // The tests below are about the name, so they use years around today -- a fixed 2026 would
    // start failing the start-date window the moment the calendar moved on.
    private static readonly int ThisYear = DateTime.UtcNow.Year;

    private static bool Accepts(string name, DateOnly start, DateOnly end) =>
        Validator().Validate(new AcademicYearCreateDto(name, start, end)).IsValid;

    private static bool AcceptsYear(int startYear, int endYear) =>
        Accepts(
            $"{startYear}/{endYear}",
            new DateOnly(startYear, 9, 1),
            new DateOnly(endYear, 6, 30));

    [Fact]
    public void A_name_that_matches_its_dates_is_accepted()
    {
        Assert.True(AcceptsYear(ThisYear, ThisYear + 1));

        Assert.True(Accepts(
            $"{ThisYear}", new DateOnly(ThisYear, 1, 10), new DateOnly(ThisYear, 12, 20)));
    }

    [Fact]
    public void The_record_that_caused_the_bug_is_rejected() =>
        Assert.False(Accepts("2026/2027", new DateOnly(2023, 1, 1), new DateOnly(2026, 9, 9)));

    // A year three back is internally consistent -- the name matches the dates -- and still
    // nonsense to be creating now. This is the rule the bad record needed and did not have.
    [Fact]
    public void A_year_far_from_today_is_rejected_even_when_its_name_fits()
    {
        Assert.False(AcceptsYear(ThisYear - 3, ThisYear - 2));
        Assert.False(AcceptsYear(ThisYear + 3, ThisYear + 4));
    }

    [Fact]
    public void The_year_either_side_of_today_is_still_allowed()
    {
        Assert.True(AcceptsYear(ThisYear - 1, ThisYear));
        Assert.True(AcceptsYear(ThisYear + 1, ThisYear + 2));
    }

    [Fact]
    public void The_window_widens_with_configuration()
    {
        var start = new DateOnly(ThisYear - 3, 9, 1);
        var end = new DateOnly(ThisYear - 2, 6, 30);
        var dto = new AcademicYearCreateDto($"{ThisYear - 3}/{ThisYear - 2}", start, end);

        Assert.False(Validator().Validate(dto).IsValid);
        Assert.True(Validator(back: 5).Validate(dto).IsValid);
    }

    [Fact]
    public void A_name_that_contradicts_its_dates_is_rejected()
    {
        // Starts in the wrong year.
        Assert.False(Accepts($"{ThisYear}/{ThisYear + 1}",
            new DateOnly(ThisYear - 1, 9, 1), new DateOnly(ThisYear + 1, 6, 30)));

        // Ends in the wrong year.
        Assert.False(Accepts($"{ThisYear}/{ThisYear + 1}",
            new DateOnly(ThisYear, 9, 1), new DateOnly(ThisYear + 2, 6, 30)));

        // Two halves that are not consecutive.
        Assert.False(Accepts($"{ThisYear}/{ThisYear + 2}",
            new DateOnly(ThisYear, 9, 1), new DateOnly(ThisYear + 2, 6, 30)));

        // A single-year name spanning two calendar years.
        Assert.False(Accepts($"{ThisYear}",
            new DateOnly(ThisYear, 9, 1), new DateOnly(ThisYear + 1, 6, 30)));
    }

    [Theory]
    [InlineData("Viti 2026")]
    [InlineData("2026-2027")]
    [InlineData("26/27")]
    [InlineData("1999/2000")]
    [InlineData("2026/2027/2028")]
    public void A_name_in_another_shape_is_rejected(string name) =>
        Assert.False(Accepts(name, new DateOnly(ThisYear, 9, 1), new DateOnly(ThisYear + 1, 6, 30)));
}
