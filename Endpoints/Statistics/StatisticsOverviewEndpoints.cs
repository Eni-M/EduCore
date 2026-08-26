using EduCore.Dtos.Statistics;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

// The two dashboard endpoints. They compose the focused services and define no metric of
// their own, so they cannot drift from what the granular endpoints report.
public static class StatisticsOverviewEndpoints
{
    public static void MapStatisticsOverviewEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/school-overview", async (
                [AsParameters] StatisticsFilter filter, IStatisticsOverviewService service, CancellationToken ct) =>
            Results.Ok(await service.GetSchoolOverviewAsync(filter, ct)));

        // Supersedes GET /api/schools/statistics.
        group.MapGet("/platform-overview", async (
                [AsParameters] StatisticsFilter filter, IStatisticsOverviewService service, CancellationToken ct) =>
                Results.Ok(await service.GetPlatformOverviewAsync(filter, ct)))
            .RequireAuthorization(Policies.SuperAdmin);
    }
}
