using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class AcademicStatisticsEndpoints
{
    public static void MapAcademicStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var academic = group.MapGroup("/academic");

        academic.MapGet("/", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetSummaryAsync(filter, ct)));

        academic.MapGet("/by-subject", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetBySubjectAsync(filter, ct)));

        academic.MapGet("/by-class", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByClassAsync(filter, ct)));

        academic.MapGet("/by-grade", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByGradeAsync(filter, ct)));

        academic.MapGet("/distribution", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetDistributionAsync(filter, ct)));

        // "Class A vs Class B, Mathematics, 4.1 vs 3.6"
        academic.MapGet("/subject-comparison/{subjectId:guid}", async (
                Guid subjectId,
                [AsParameters] StatisticsFilter filter,
                IAcademicStatisticsService service,
                CancellationToken ct) =>
            Results.Ok(await service.GetSubjectComparisonAsync(filter, subjectId, ct)));

        // individual -- names students
        academic.MapGet("/at-risk", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetAtRiskAsync(filter, ct)));
    }
}
