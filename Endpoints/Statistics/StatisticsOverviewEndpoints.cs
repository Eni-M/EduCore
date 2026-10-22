using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class StatisticsOverviewEndpoints
{
    public static void MapStatisticsOverviewEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/school-overview", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IStatisticsOverviewService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? TypedResults.Ok(await service.GetSchoolOverviewAsync(filter, ct))
                : TypedResults.Ok(await service.GetSchoolOverviewAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        // The per-school comparison table is one row per school nationwide. Paging params page
        // it in the database; without them the full list is returned, for chart and export
        // callers that need every school at once.
        group.MapGet("/platform-overview", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IStatisticsOverviewService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetPlatformOverviewAsync(
                    filter,
                    page is null && pageSize is null
                        ? null
                        : new PaginationQueryDto(page ?? 1, pageSize ?? 20),
                    ct)))
            .RequireAuthorization(Policies.StateAdmin);
    }
}
