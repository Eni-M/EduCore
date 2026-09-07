using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class StudentStatisticsEndpoints
{
    public static void MapStudentStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var students = group.MapGroup("/students");

        students.MapGet("/", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetSummaryAsync(filter, ct)));

        // Bounded by the grade range a school can teach (about a dozen rows), so it is not
        // paged: a page parameter would add a round trip and break the chart that reads it.
        students.MapGet("/by-grade", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByGradeAsync(filter, ct)));

        // One row per class, which across a region is thousands. Paged in the database;
        // no paging params → full list, for chart and export callers.
        students.MapGet("/by-class", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetByClassAsync(filter, ct))
                : Results.Ok(await service.GetByClassPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        // One row per distinct age — a handful. Bounded, so not paged.
        students.MapGet("/by-age", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByAgeAsync(filter, ct)));

        // One point per month in the selected window. Bounded, so not paged.
        students.MapGet("/enrollment-trend", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetEnrollmentTrendAsync(filter, ct)));
    }
}
