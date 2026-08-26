using EduCore.Dtos.Statistics;
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

        // individual -- names students
        attendance.MapGet("/by-student", async (
                [AsParameters] StatisticsFilter filter, IAttendanceStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByStudentAsync(filter, ct)));

        // individual -- names students
        attendance.MapGet("/excessive-absences", async (
                [AsParameters] StatisticsFilter filter,
                double? thresholdPercent,
                IAttendanceStatisticsService service,
                CancellationToken ct) =>
            Results.Ok(await service.GetExcessiveAbsencesAsync(filter, thresholdPercent, ct)));
    }
}
