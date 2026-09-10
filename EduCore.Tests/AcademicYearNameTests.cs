using EduCore.Dtos.AcademicYear;
using EduCore.Validation.AcademicYear;
using Xunit;

namespace EduCore.Tests;

// The dev database holds a year named "2026/2027" that actually ran 2023-01-01 to 2026-09-09.
// Its second term ended the day before a teacher tried to record a mark, and all they were
// told was that today fell outside every term. These are the rules that make that unwritable.
public class AcademicYearNameTests
{
    private static readonly AcademicYearCreateDtoValidator Validator = new();

    private static bool Accepts(string name, DateOnly start, DateOnly end) =>
        Validator.Validate(new AcademicYearCreateDto(name, start, end)).IsValid;

    [Theory]
    [InlineData("2026/2027", 2026, 9, 1, 2027, 6, 30)]
    [InlineData("2026", 2026, 1, 10, 2026, 12, 20)]
    public void A_name_that_matches_its_dates_is_accepted(
        string name, int y1, int m1, int d1, int y2, int m2, int d2) =>
        Assert.True(Accepts(name, new DateOnly(y1, m1, d1), new DateOnly(y2, m2, d2)));

    [Fact]
    public void The_record_that_caused_the_bug_is_rejected() =>
        Assert.False(Accepts("2026/2027", new DateOnly(2023, 1, 1), new DateOnly(2026, 9, 9)));

    [Theory]
    // Starts in the wrong year.
    [InlineData("2026/2027", 2025, 9, 1, 2027, 6, 30)]
    // Ends in the wrong year.
    [InlineData("2026/2027", 2026, 9, 1, 2028, 6, 30)]
    // Two halves that are not consecutive.
    [InlineData("2026/2028", 2026, 9, 1, 2028, 6, 30)]
    // A single-year name spanning two calendar years.
    [InlineData("2026", 2026, 9, 1, 2027, 6, 30)]
    public void A_name_that_contradicts_its_dates_is_rejected(
        string name, int y1, int m1, int d1, int y2, int m2, int d2) =>
        Assert.False(Accepts(name, new DateOnly(y1, m1, d1), new DateOnly(y2, m2, d2)));

    [Theory]
    [InlineData("Viti 2026")]
    [InlineData("2026-2027")]
    [InlineData("26/27")]
    [InlineData("1999/2000")]
    [InlineData("2026/2027/2028")]
    public void A_name_in_another_shape_is_rejected(string name) =>
        Assert.False(Accepts(name, new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30)));
}
