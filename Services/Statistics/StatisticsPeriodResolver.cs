using EduCore.Configuration;
using EduCore.Dtos.Statistics;
using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

public class StatisticsPeriodResolver(
    IAcademicYearRepository academicYears,
    IGradingPeriodRepository gradingPeriods,
    IOptions<StatisticsQueryOptions> options) : IStatisticsPeriodResolver
{
    private readonly int _maximumWindowYears = options.Value.MaximumWindowYears;

    public async Task<(DateOnly From, DateOnly To)> ResolveAsync(
        StatisticsFilter filter, Guid? schoolId, CancellationToken ct)
    {
        var hasPeriodReference = filter.GradingPeriodId is not null || filter.AcademicYearId is not null;
        var hasRawRange = filter.From is not null || filter.To is not null;

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

            var from = filter.From ?? to.AddYears(-1);

            if (from > to)
                throw new BusinessException("The start of the range cannot be after its end.");

            if (from < to.AddYears(-_maximumWindowYears))
                throw new BusinessException(
                    $"A statistics window cannot be longer than {_maximumWindowYears} years.");

            return (from, to);
        }

        return await CurrentAcademicYearAsync(schoolId, ct);
    }

    private async Task<(DateOnly, DateOnly)> CurrentAcademicYearAsync(Guid? schoolId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (schoolId is not null)
        {
            var current = await academicYears.GetCurrentAsync(schoolId.Value, today, ct);

            if (current is not null)
                return (current.StartDate, current.EndDate);
        }

        return (today.AddYears(-1), today);
    }

    private static void EnsureSameSchool(Guid ownerSchoolId, Guid? schoolId, string what, Guid id)
    {
        if (schoolId is not null && ownerSchoolId != schoolId)
            throw new NotFoundException(what, id);
    }
}
