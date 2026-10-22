using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class ClassStatisticsEndpoints
{
    public static void MapClassStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var classes = group.MapGroup("/classes");

        // One row per class in scope, which across a region is thousands. Paged in the
        // database; no paging params → full list, for chart and export callers.
        classes.MapGet("/summary", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IClassStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetSummariesAsync(filter, ct))
                : Results.Ok(await service.GetSummariesPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        classes.MapGet("/{classId:guid}", async (
                Guid classId,
                [AsParameters] StatisticsFilter filter,
                IClassStatisticsService service,
                CancellationToken ct) =>
            TypedResults.Ok(await service.GetDetailAsync(filter, classId, ct)));
    }
}
