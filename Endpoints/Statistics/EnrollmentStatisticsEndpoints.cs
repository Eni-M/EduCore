using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class EnrollmentStatisticsEndpoints
{
    public static void MapEnrollmentStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var enrollment = group.MapGroup("/enrollment");

        enrollment.MapGet("/", async (
                [AsParameters] StatisticsFilter filter, IEnrollmentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetSummaryAsync(filter, ct)));

        // One row per academic year, and one per exit reason: both bounded by the domain, so
        // neither is paged. A page parameter would cost a round trip and buy nothing.
        enrollment.MapGet("/by-year", async (
                [AsParameters] StatisticsFilter filter, IEnrollmentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByAcademicYearAsync(filter, ct)));

        enrollment.MapGet("/exits-by-reason", async (
                [AsParameters] StatisticsFilter filter, IEnrollmentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetExitsByReasonAsync(filter, ct)));

        // One row per class in scope — thousands across a region. Paged in the database;
        // no paging params → full list, for chart and export callers.
        enrollment.MapGet("/capacity", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IEnrollmentStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetCapacityAsync(filter, ct))
                : Results.Ok(await service.GetCapacityPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));
    }
}
