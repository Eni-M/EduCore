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
            Results.Ok(await service.GetSummaryAsync(filter, ct)));

        attendance.MapGet("/by-class", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByClassAsync(filter, ct)));

        attendance.MapGet("/by-grade", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByGradeAsync(filter, ct)));

        attendance.MapGet("/by-subject", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetBySubjectAsync(filter, ct)));

        attendance.MapGet("/trend", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetTrendAsync(filter, ct)));

        // Per-student rows across the whole scope — vast. Page for the table; no paging params → full list.
        // paging trims the payload, not the DB read; push into SQL if the aggregation gets slow.
        attendance.MapGet("/by-student", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
        {
            var all = await service.GetByStudentAsync(filter, ct);
            return page is null && pageSize is null
                ? Results.Ok(all)
                : Results.Ok(all.ToPagedResult(new PaginationQueryDto(page ?? 1, pageSize ?? 20)));
        });

        attendance.MapGet("/excessive-absences", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter,
                double? thresholdPercent,
                IAttendanceStatisticsService service,
                CancellationToken ct) =>
        {
            var all = await service.GetExcessiveAbsencesAsync(filter, thresholdPercent, ct);
            return page is null && pageSize is null
                ? Results.Ok(all)
                : Results.Ok(all.ToPagedResult(new PaginationQueryDto(page ?? 1, pageSize ?? 20)));
        });
    }
}
