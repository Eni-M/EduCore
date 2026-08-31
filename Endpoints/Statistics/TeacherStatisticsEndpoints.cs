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
            Results.Ok(await service.GetSummaryAsync(filter, ct)));

        teachers.MapGet("/workload", async (
                [AsParameters] StatisticsFilter filter, ITeacherStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetWorkloadAsync(filter, ct)));

        teachers.MapGet("/coverage", async (
                [AsParameters] StatisticsFilter filter, ITeacherStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetCoverageAsync(filter, ct)));

        teachers.MapGet("/{teacherId:guid}", async (
                Guid teacherId,
                [AsParameters] StatisticsFilter filter,
                ITeacherStatisticsService service,
                CancellationToken ct) =>
            Results.Ok(await service.GetDetailAsync(filter, teacherId, ct)));
    }
}
