using EduCore.Dtos.Statistics;
using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

// Turns any filter into a concrete calendar window. Day, week, month, year and ten-year
// reports are all callers handing this a different window; without it every query would be
// locked to school-defined academic years, which do not line up with calendar periods.
public class StatisticsPeriodResolver(
    IAcademicYearRepository academicYears,
    IGradingPeriodRepository gradingPeriods) : IStatisticsPeriodResolver
{
    private const int MaximumWindowYears = 10;

    public async Task<(DateOnly From, DateOnly To)> ResolveAsync(
        StatisticsFilter filter, Guid? schoolId, CancellationToken ct)
    {
        var hasPeriodReference = filter.GradingPeriodId is not null || filter.AcademicYearId is not null;
        var hasRawRange = filter.From is not null || filter.To is not null;

        // Refused rather than resolved by a silent precedence rule: a caller who supplies both
        // has misunderstood something, and quietly picking one hides that.
        if (hasPeriodReference && hasRawRange)
            throw new BusinessException(
                "Supply either a grading period / academic year, or a from/to date range -- not both.");

        if (filter.GradingPeriodId is not null)
        {
            var period = await gradingPeriods.GetByIdAcrossSchoolsAsync(filter.GradingPeriodId.Value, ct)
                         ?? throw new NotFoundException("Grading period", filter.GradingPeriodId.Value);

            EnsureSameSchool(period.SchoolId, schoolId, "Grading period", filter.GradingPeriodId.Value);

            return (period.StartDate, period.EndDate);
        }

        if (filter.AcademicYearId is not null)
        {
            var year = await academicYears.GetByIdAcrossSchoolsAsync(filter.AcademicYearId.Value, ct)
                       ?? throw new NotFoundException("Academic year", filter.AcademicYearId.Value);

            EnsureSameSchool(year.SchoolId, schoolId, "Academic year", filter.AcademicYearId.Value);

            return (year.StartDate, year.EndDate);
        }

        if (hasRawRange)
        {
            var to = filter.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // A year back, not DateOnly.MinValue: an open-ended start would make the monthly
            // enrollment trend walk two thousand years of months.
            var from = filter.From ?? to.AddYears(-1);

            if (from > to)
                throw new BusinessException("The start of the range cannot be after its end.");

            if (from < to.AddYears(-MaximumWindowYears))
                throw new BusinessException(
                    $"A statistics window cannot be longer than {MaximumWindowYears} years.");

            return (from, to);
        }

        return await CurrentAcademicYearAsync(schoolId, ct);
    }

    // Falls back to the calendar year for a school with no years defined yet, and for
    // cross-school queries where no single year is "current".
    private async Task<(DateOnly, DateOnly)> CurrentAcademicYearAsync(Guid? schoolId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (schoolId is not null)
        {
            var current = await academicYears.GetCurrentAsync(schoolId.Value, today, ct);

            if (current is not null)
                return (current.StartDate, current.EndDate);
        }

        return (new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31));
    }

    // Not found rather than forbidden, unlike the SchoolId check in StatisticsScopeResolver.
    // Answering "forbidden" for a period that exists elsewhere would tell an admin which ids
    // are real in other schools.
    private static void EnsureSameSchool(Guid ownerSchoolId, Guid? schoolId, string what, Guid id)
    {
        if (schoolId is not null && ownerSchoolId != schoolId)
            throw new NotFoundException(what, id);
    }
}
