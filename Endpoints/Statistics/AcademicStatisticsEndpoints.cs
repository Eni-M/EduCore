using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Helpers;
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

        academic.MapGet("/subject-comparison/{subjectId:guid}", async (
                Guid subjectId,
                [AsParameters] StatisticsFilter filter,
                IAcademicStatisticsService service,
                CancellationToken ct) =>
            Results.Ok(await service.GetSubjectComparisonAsync(filter, subjectId, ct)));

        // At-risk is one row per student across the whole scope, so it can be vast; page it for the
        // table. No paging params → full list (chart/export callers). paging trims the
        // client payload, not the DB read; push into SQL only if the aggregation itself gets slow.
        academic.MapGet("/at-risk", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
        {
            var all = await service.GetAtRiskAsync(filter, ct);
            return page is null && pageSize is null
                ? Results.Ok(all)
                : Results.Ok(all.ToPagedResult(new PaginationQueryDto(page ?? 1, pageSize ?? 20)));
        });
    }
}
