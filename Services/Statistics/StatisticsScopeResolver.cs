using EduCore.Dtos.Statistics;
using EduCore.Exceptions;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class StatisticsScopeResolver(
    ICurrentUser currentUser,
    IStatisticsPeriodResolver periods) : IStatisticsScopeResolver
{
    public async Task<StatisticsQuery> ResolveAsync(StatisticsFilter filter, CancellationToken ct)
    {
        filter = ApplyTierScope(filter);

        var (schoolId, crossSchool) = ResolveSchool(filter);
        var (from, to) = await periods.ResolveAsync(filter, schoolId, ct);

        return new StatisticsQuery(
            schoolId,
            crossSchool,
            from,
            to,
            filter.AcademicYearId,
            filter.GradingPeriodId,
            filter.ClassId,
            filter.Grade,
            filter.SubjectId,
            filter.TeacherId,
            filter.CityId,
            filter.ZoneId,
            filter.StateId,
            currentUser.IsStateAdmin ? currentUser.RequireStateId() : null);
    }

    private StatisticsFilter ApplyTierScope(StatisticsFilter filter) =>
        currentUser switch
        {
            { IsRegionalAdmin: true } => filter with
            {
                SchoolId = null,
                CityId = currentUser.RequireCityId(),
                ZoneId = null,
                StateId = null
            },
            _ => filter
        };

    private (Guid? SchoolId, bool CrossSchool) ResolveSchool(StatisticsFilter filter)
    {
        if (currentUser.IsStateAdmin || currentUser.IsRegionalAdmin)
            return (filter.SchoolId, filter.SchoolId is null);

        var ownSchoolId = currentUser.RequireSchoolId();

        if (filter.SchoolId is not null && filter.SchoolId != ownSchoolId)
            throw new ForbiddenException("You can only view statistics for your own school.");

        return (ownSchoolId, false);
    }
}
