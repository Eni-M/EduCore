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

        enrollment.MapGet("/by-year", async (
                [AsParameters] StatisticsFilter filter, IEnrollmentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByAcademicYearAsync(filter, ct)));

        enrollment.MapGet("/exits-by-reason", async (
                [AsParameters] StatisticsFilter filter, IEnrollmentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetExitsByReasonAsync(filter, ct)));

        enrollment.MapGet("/capacity", async (
                [AsParameters] StatisticsFilter filter, IEnrollmentStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetCapacityAsync(filter, ct)));
    }
}
