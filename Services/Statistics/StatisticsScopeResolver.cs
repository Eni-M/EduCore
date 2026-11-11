using EduCore.Dtos.Statistics;
using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class StatisticsScopeResolver(
    ICurrentUser currentUser,
    IStatisticsPeriodResolver periods,
    ISchoolRepository schools) : IStatisticsScopeResolver
{
    public async Task<StatisticsQuery> ResolveAsync(StatisticsFilter filter, CancellationToken ct)
    {
        filter = ApplyTierScope(filter);

        // A regional admin may narrow to one school, but only one in their own city.
        if (currentUser.IsRegionalAdmin && filter.SchoolId is { } chosen)
        {
            var school = await schools.GetWithLocationAsync(chosen, ct);
            if (school?.CityId != currentUser.RequireCityId())
                throw new ForbiddenException("This school is not in your city.");
        }

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
            // Student origin — filters students by where their family comes from.
            filter.CountryId,
            filter.OriginCityId,
            // The ministry (StateAdmin) sees every school nationwide; a StateId filter, when given,
            // narrows to that school region. Everyone else has no school-location-state scope of their own.
            currentUser.IsStateAdmin ? filter.StateId : null);
    }

    private StatisticsFilter ApplyTierScope(StatisticsFilter filter) =>
        currentUser switch
        {
            // The chosen school is kept (and checked against the city above); before, it was
            // dropped, so picking a school on the statistics page changed nothing.
            { IsRegionalAdmin: true } => filter with
            {
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
