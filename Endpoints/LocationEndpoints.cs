using EduCore.Dtos.Location;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class LocationEndpoints
{
    public static void MapLocationEndpoints(this IEndpointRouteBuilder api)
    {
        var states = api.MapGroup("/states").WithTags("Locations");

        states.MapGet("/", async (ILocationService service, CancellationToken ct) =>
                Results.Ok(await service.GetStatesAsync(ct)))
            .RequireAuthorization();

        states.MapPost("/", async (StateWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var state = await service.CreateStateAsync(dto, ct);
                return Results.Created($"/api/states/{state.Id}", state);
            })
            .RequireAuthorization(Policies.SuperAdmin);

        states.MapPut("/{stateId:guid}",
                async (Guid stateId, StateWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateStateAsync(stateId, dto, ct)))
            .RequireAuthorization(Policies.SuperAdmin);

        states.MapDelete("/{stateId:guid}",
                async (Guid stateId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteStateAsync(stateId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.SuperAdmin);

        var cities = api.MapGroup("/cities").WithTags("Locations");

        cities.MapGet("/", async (Guid? stateId, ILocationService service, CancellationToken ct) =>
                Results.Ok(await service.GetCitiesAsync(stateId, ct)))
            .RequireAuthorization();

        cities.MapPost("/", async (CityWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var city = await service.CreateCityAsync(dto, ct);
                return Results.Created($"/api/cities/{city.Id}", city);
            })
            .RequireAuthorization(Policies.SuperAdmin);

        cities.MapPut("/{cityId:guid}",
                async (Guid cityId, CityWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateCityAsync(cityId, dto, ct)))
            .RequireAuthorization(Policies.SuperAdmin);

        cities.MapDelete("/{cityId:guid}",
                async (Guid cityId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteCityAsync(cityId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.SuperAdmin);

        var zones = api.MapGroup("/zones").WithTags("Locations");

        zones.MapGet("/", async (Guid? cityId, ILocationService service, CancellationToken ct) =>
                Results.Ok(await service.GetZonesAsync(cityId, ct)))
            .RequireAuthorization();

        zones.MapPost("/", async (ZoneWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var zone = await service.CreateZoneAsync(dto, ct);
                return Results.Created($"/api/zones/{zone.Id}", zone);
            })
            .RequireAuthorization(Policies.SuperAdmin);

        zones.MapPut("/{zoneId:guid}",
                async (Guid zoneId, ZoneWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateZoneAsync(zoneId, dto, ct)))
            .RequireAuthorization(Policies.SuperAdmin);

        zones.MapDelete("/{zoneId:guid}",
                async (Guid zoneId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteZoneAsync(zoneId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.SuperAdmin);
    }
}
