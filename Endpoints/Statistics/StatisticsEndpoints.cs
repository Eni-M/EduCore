using EduCore.Security;

namespace EduCore.Endpoints.Statistics;

// The statistics surface, split one file per area so a change to one group of metrics touches
// exactly one file.
//
// Granular by design: one endpoint per metric family, so a slice can later be made public by
// routing and authorization alone rather than by restructuring anything.
//
// Endpoints marked "individual" return records naming a student or a teacher. Those can never
// be part of a public slice, and small-cell suppression cannot protect them -- see
// DTOs/Statistics/Individual.
public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/statistics")
            .RequireAuthorization(Policies.AdminOrSuperAdmin)
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
