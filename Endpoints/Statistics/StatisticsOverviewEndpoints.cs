using EduCore.Dtos.Statistics;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class StatisticsOverviewEndpoints
{
    public static void MapStatisticsOverviewEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/school-overview", async (
                [AsParameters] StatisticsFilter filter, IStatisticsOverviewService service, CancellationToken ct) =>
            Results.Ok(await service.GetSchoolOverviewAsync(filter, ct)));

        group.MapGet("/platform-overview", async (
                [AsParameters] StatisticsFilter filter, IStatisticsOverviewService service, CancellationToken ct) =>
                Results.Ok(await service.GetPlatformOverviewAsync(filter, ct)))
            .RequireAuthorization(Policies.SuperAdmin);
    }
}
