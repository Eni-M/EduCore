using EduCore.Dtos.Statistics;
using EduCore.Exceptions;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

// Every statistics service takes a StatisticsContext, never a raw StatisticsFilter, so a
// query cannot be run against input that has not been through here first.
public class StatisticsScopeResolver(
    ICurrentUser currentUser,
    IStatisticsPeriodResolver periods) : IStatisticsScopeResolver
{
    public async Task<StatisticsContext> ResolveAsync(StatisticsFilter filter, CancellationToken ct)
    {
        var (schoolId, crossSchool) = ResolveSchool(filter);
        var (from, to) = await periods.ResolveAsync(filter, schoolId, ct);

        return new StatisticsContext(
            schoolId,
            crossSchool,
            from,
            to,
            filter.AcademicYearId,
            filter.GradingPeriodId,
            filter.ClassId,
            filter.Grade,
            filter.SubjectId,
            filter.TeacherId);
    }

    private (Guid? SchoolId, bool CrossSchool) ResolveSchool(StatisticsFilter filter)
    {
        if (currentUser.IsSuperAdmin)
            // No school id means every school, aggregated.
            return (filter.SchoolId, filter.SchoolId is null);

        var ownSchoolId = currentUser.RequireSchoolId();

        // Refused, not quietly rewritten to their own school: coercing it would keep the
        // attempt out of the audit log, which is exactly where it belongs.
        if (filter.SchoolId is not null && filter.SchoolId != ownSchoolId)
            throw new ForbiddenException("You can only view statistics for your own school.");

        return (ownSchoolId, false);
    }
}
