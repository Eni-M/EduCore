using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class TeacherStatisticsEndpoints
{
    public static void MapTeacherStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var teachers = group.MapGroup("/teachers");

        teachers.MapGet("/", async (
                [AsParameters] StatisticsFilter filter, ITeacherStatisticsService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetSummaryAsync(filter, ct)));

        // One row per teacher in scope, which across a region is thousands. Paged in the
        // database; no paging params → full list, for chart and export callers.
        teachers.MapGet("/workload", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, ITeacherStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetWorkloadAsync(filter, ct))
                : Results.Ok(await service.GetWorkloadPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        teachers.MapGet("/coverage", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, ITeacherStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetCoverageAsync(filter, ct))
                : Results.Ok(await service.GetCoveragePagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        teachers.MapGet("/{teacherId:guid}", async (
                Guid teacherId,
                [AsParameters] StatisticsFilter filter,
                ITeacherStatisticsService service,
                CancellationToken ct) =>
            TypedResults.Ok(await service.GetDetailAsync(filter, teacherId, ct)));
    }
}
