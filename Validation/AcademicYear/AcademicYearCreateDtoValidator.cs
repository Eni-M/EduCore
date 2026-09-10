using System.Text.RegularExpressions;
using EduCore.Dtos.AcademicYear;
using EduCore.Helpers;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EduCore.Validation.AcademicYear;

// The name and the dates are two statements of the same fact, and nothing used to check they
// agreed. The dev database holds the result: a year named "2026/2027" running 2023-01-01 to
// 2026-09-09, whose second term ended the day before a teacher tried to enter a mark and got
// "today does not fall inside any term".
public partial class AcademicYearCreateDtoValidator : AbstractValidator<AcademicYearCreateDto>
{
    // "2026" or "2026/2027". The century is fixed deliberately -- it is what makes a
    // transposed year impossible to write and lets the name be checked against the dates.
    [GeneratedRegex(@"^20\d{2}(/20\d{2})?$")]
    private static partial Regex NamePattern();

    public AcademicYearCreateDtoValidator(IOptions<AcademicCalendarOptions> calendar)
    {
        var window = calendar.Value;

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(NamePattern())
            .WithMessage("An academic year is named like 2026 or 2026/2027.");

        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);

        // Matching the dates is not enough on its own: "2023/2024" with dates to match is
        // internally consistent and still nonsense to be creating in 2026. A year starts within
        // reach of today -- see AcademicCalendar:EarliestStartYearsAgo / LatestStartYearsAhead.
        RuleFor(x => x.StartDate)
            .Must(start =>
            {
                var thisYear = DateTime.UtcNow.Year;

                return start.Year >= thisYear - window.EarliestStartYearsAgo
                       && start.Year <= thisYear + window.LatestStartYearsAhead;
            })
            .WithMessage("An academic year must start close to the current one.");

        RuleFor(x => x.Name)
            .Must((dto, name) => NameAgreesWithDates(name, dto.StartDate, dto.EndDate))
            .When(x => NamePattern().IsMatch(x.Name ?? string.Empty) && x.EndDate > x.StartDate)
            .WithMessage("The name of an academic year must match the years it starts and ends in.");
    }

    private static bool NameAgreesWithDates(string name, DateOnly startDate, DateOnly endDate)
    {
        var parts = name.Split('/');

        if (!int.TryParse(parts[0], out var first))
            return false;

        if (first != startDate.Year)
            return false;

        // One part: a year that opens and closes in the same calendar year.
        if (parts.Length == 1)
            return endDate.Year == startDate.Year;

        return int.TryParse(parts[1], out var second)
               && second == first + 1
               && endDate.Year == second;
    }
}
