using EduCore.Security;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Endpoints.Statistics;

public static class DemographicsStatisticsEndpoints
{
    public static void MapDemographicsStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var demographics = group.MapGroup("/demographics")
            .RequireAuthorization(Policies.SuperAdmin);

        demographics.MapGet("/", async (IDemographicsStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetOverviewAsync(ct)));

        demographics.MapGet("/by-zone", async (
                string? city, IDemographicsStatisticsService service, CancellationToken ct) =>
            Results.Ok(await service.GetByZoneAsync(city, ct)));
    }
}
