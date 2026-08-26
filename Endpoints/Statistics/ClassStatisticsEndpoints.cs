using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class ClassStatisticsEndpoints
{
    public static void MapClassStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var classes = group.MapGroup("/classes");

        classes.MapGet("/summary", async (
                [AsParameters] StatisticsFilter filter, IClassStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetSummariesAsync(filter, ct)));

        classes.MapGet("/{classId:guid}", async (
                Guid classId,
                [AsParameters] StatisticsFilter filter,
                IClassStatisticsService service,
                CancellationToken ct) =>
            Results.Ok(await service.GetDetailAsync(filter, classId, ct)));
    }
}
