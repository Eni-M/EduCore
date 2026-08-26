using EduCore.Dtos.Statistics;
using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

// Turns any filter into a concrete calendar window.
//
// This is the piece that makes government reporting possible later: day, week, month, year
// and ten-year reports are all just callers handing this resolver a different window. Without
// it, every statistics query would be locked to school-defined academic years, which do not
// line up with the calendar periods a ministry asks for.
public class StatisticsPeriodResolver(
    IAcademicYearRepository academicYears,
    IGradingPeriodRepository gradingPeriods) : IStatisticsPeriodResolver
{
    public async Task<(DateOnly From, DateOnly To)> ResolveAsync(
        StatisticsFilter filter, Guid? schoolId, CancellationToken ct)
    {
        var hasPeriodReference = filter.GradingPeriodId is not null || filter.AcademicYearId is not null;
        var hasRawRange = filter.From is not null || filter.To is not null;

        // Refused rather than resolved by a silent precedence rule -- a caller who supplies
        // both has misunderstood something, and quietly picking one hides that.
        if (hasPeriodReference && hasRawRange)
            throw new BusinessException(
                "Supply either a grading period / academic year, or a from/to date range -- not both.");

        if (filter.GradingPeriodId is not null)
        {
            var period = await gradingPeriods.GetByIdAcrossSchoolsAsync(filter.GradingPeriodId.Value, ct)
                         ?? throw new NotFoundException("Grading period", filter.GradingPeriodId.Value);

            EnsureSameSchool(period.SchoolId, schoolId);

            return (period.StartDate, period.EndDate);
        }

        if (filter.AcademicYearId is not null)
        {
            var year = await academicYears.GetByIdAcrossSchoolsAsync(filter.AcademicYearId.Value, ct)
                       ?? throw new NotFoundException("Academic year", filter.AcademicYearId.Value);

            EnsureSameSchool(year.SchoolId, schoolId);

            return (year.StartDate, year.EndDate);
        }

        if (hasRawRange)
        {
            var from = filter.From ?? DateOnly.MinValue;
            var to = filter.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

            if (from > to)
                throw new BusinessException("The start of the range cannot be after its end.");

            return (from, to);
        }

        return await CurrentAcademicYearAsync(schoolId, ct);
    }

    // Default window. For a single school that is its current academic year; falling back to
    // the calendar year covers a brand-new school with no years defined yet, and cross-school
    // queries, where schools' years genuinely differ and no single one is "current".
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

    private static void EnsureSameSchool(Guid ownerSchoolId, Guid? schoolId)
    {
        // A SuperAdmin querying across schools (schoolId null) may reference any period.
        if (schoolId is not null && ownerSchoolId != schoolId)
            throw new ForbiddenException("That period belongs to a different school.");
    }
}
