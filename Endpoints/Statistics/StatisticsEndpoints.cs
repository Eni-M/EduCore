using EduCore.Security;

namespace EduCore.Endpoints.Statistics;

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/statistics")
            .RequireAuthorization(Policies.StatisticsViewer)
            .WithTags("Statistics");

        group.MapStudentStatisticsEndpoints();
        group.MapEnrollmentStatisticsEndpoints();
        group.MapAttendanceStatisticsEndpoints();
        group.MapAcademicStatisticsEndpoints();
        group.MapTeacherStatisticsEndpoints();
        group.MapClassStatisticsEndpoints();
        group.MapDemographicsStatisticsEndpoints();
        group.MapStatisticsOverviewEndpoints();
    }
}
