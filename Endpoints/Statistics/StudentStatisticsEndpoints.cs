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

        students.MapGet("/by-grade", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByGradeAsync(filter, ct)));

        students.MapGet("/by-class", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByClassAsync(filter, ct)));

        students.MapGet("/by-age", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByAgeAsync(filter, ct)));

        students.MapGet("/enrollment-trend", async (
                [AsParameters] StatisticsFilter filter, IStudentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetEnrollmentTrendAsync(filter, ct)));
    }
}
