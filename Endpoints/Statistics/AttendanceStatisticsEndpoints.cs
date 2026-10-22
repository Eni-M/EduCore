using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Helpers;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class AttendanceStatisticsEndpoints
{
    public static void MapAttendanceStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var attendance = group.MapGroup("/attendance");

        attendance.MapGet("/", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetSummaryAsync(filter, ct)));

        // One row per class in scope — thousands across a region. Paged in the database;
        // no paging params → full list, for chart and export callers.
        attendance.MapGet("/by-class", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetByClassAsync(filter, ct))
                : Results.Ok(await service.GetByClassPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        attendance.MapGet("/by-grade", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetByGradeAsync(filter, ct)));

        attendance.MapGet("/by-subject", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetBySubjectAsync(filter, ct))
                : Results.Ok(await service.GetBySubjectPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        attendance.MapGet("/trend", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetTrendAsync(filter, ct)));

        // Per-student rows across the whole scope — vast. Paged in the database; no paging
        // params → full list, for the chart and export callers that need every student.
        attendance.MapGet("/by-student", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetByStudentAsync(filter, ct))
                : Results.Ok(await service.GetByStudentPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        attendance.MapGet("/excessive-absences", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter,
                double? thresholdPercent,
                IAttendanceStatisticsService service,
                CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetExcessiveAbsencesAsync(filter, thresholdPercent, ct))
                : Results.Ok(await service.GetExcessiveAbsencesPagedAsync(
                    filter, thresholdPercent, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));
    }
}
