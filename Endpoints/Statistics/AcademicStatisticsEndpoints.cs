using EduCore.Dtos.Pagination;
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
            TypedResults.Ok(await service.GetSummaryAsync(filter, ct)));

        // Subjects and classes both scale with the size of the scope, so both are paged in the
        // database. No paging params → full list, for the chart and export callers.
        academic.MapGet("/by-subject", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetBySubjectAsync(filter, ct))
                : Results.Ok(await service.GetBySubjectPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        academic.MapGet("/by-class", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetByClassAsync(filter, ct))
                : Results.Ok(await service.GetByClassPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));

        // Grade levels and the 1-5 mark scale are both fixed by the domain — about a dozen
        // rows and five buckets. Not paged: the charts read them whole.
        academic.MapGet("/by-grade", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetByGradeAsync(filter, ct)));

        academic.MapGet("/distribution", async (
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetDistributionAsync(filter, ct)));

        academic.MapGet("/subject-comparison/{subjectId:guid}", async (
                Guid subjectId,
                [AsParameters] StatisticsFilter filter,
                IAcademicStatisticsService service,
                CancellationToken ct) =>
            TypedResults.Ok(await service.GetSubjectComparisonAsync(filter, subjectId, ct)));

        // At-risk is one row per student across the whole scope, so it can be vast; page it for the
        // table. No paging params → full list (chart/export callers).
        academic.MapGet("/at-risk", async (
                int? page, int? pageSize,
                [AsParameters] StatisticsFilter filter, IAcademicStatisticsService service, CancellationToken ct) =>
            page is null && pageSize is null
                ? Results.Ok(await service.GetAtRiskAsync(filter, ct))
                : Results.Ok(await service.GetAtRiskPagedAsync(
                    filter, new PaginationQueryDto(page ?? 1, pageSize ?? 20), ct)));
    }
}
