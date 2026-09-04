using EduCore.Dtos.Location;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class LocationEndpoints
{
    public static void MapLocationEndpoints(this IEndpointRouteBuilder api)
    {
        var states = api.MapGroup("/states").WithTags("Locations");

        states.MapGet("/", async (int? page, int? pageSize, string? search,
                ILocationService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(await service.GetStatesAsync(ct))
                    : Results.Ok(await service.GetStatesPagedAsync(
                        new PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)))
            .RequireAuthorization();

        states.MapPost("/", async (StateWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var state = await service.CreateStateAsync(dto, ct);
                return Results.Created($"/api/states/{state.Id}", state);
            })
            .RequireAuthorization(Policies.StateAdmin);

        states.MapPut("/{stateId:guid}",
                async (Guid stateId, StateWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateStateAsync(stateId, dto, ct)))
            .RequireAuthorization(Policies.StateAdmin);

        states.MapDelete("/{stateId:guid}",
                async (Guid stateId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteStateAsync(stateId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.StateAdmin);

        var cities = api.MapGroup("/cities").WithTags("Locations");

        cities.MapGet("/", async (Guid? stateId, int? page, int? pageSize, string? search,
                ILocationService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(await service.GetCitiesAsync(stateId, ct))
                    : Results.Ok(await service.GetCitiesPagedAsync(
                        stateId, new Dtos.Pagination.PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)))
            .RequireAuthorization();

        cities.MapPost("/", async (CityWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var city = await service.CreateCityAsync(dto, ct);
                return Results.Created($"/api/cities/{city.Id}", city);
            })
            .RequireAuthorization(Policies.StateAdmin);

        cities.MapPut("/{cityId:guid}",
                async (Guid cityId, CityWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateCityAsync(cityId, dto, ct)))
            .RequireAuthorization(Policies.StateAdmin);

        cities.MapDelete("/{cityId:guid}",
                async (Guid cityId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteCityAsync(cityId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.StateAdmin);

        var zones = api.MapGroup("/zones").WithTags("Locations");

        zones.MapGet("/", async (Guid? cityId, int? page, int? pageSize, string? search,
                ILocationService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(await service.GetZonesAsync(cityId, ct))
                    : Results.Ok(await service.GetZonesPagedAsync(
                        cityId, new PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)))
            .RequireAuthorization();

        zones.MapPost("/", async (ZoneWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var zone = await service.CreateZoneAsync(dto, ct);
                return Results.Created($"/api/zones/{zone.Id}", zone);
            })
            .RequireAuthorization(Policies.StateAdmin);

        zones.MapPut("/{zoneId:guid}",
                async (Guid zoneId, ZoneWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateZoneAsync(zoneId, dto, ct)))
            .RequireAuthorization(Policies.StateAdmin);

        zones.MapDelete("/{zoneId:guid}",
                async (Guid zoneId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteZoneAsync(zoneId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.StateAdmin);

        // Student origin — countries and their cities. Read by anyone signed in
        // (the student form needs the list); managed by the ministry.
        var countries = api.MapGroup("/countries").WithTags("Locations");

        countries.MapGet("/", async (int? page, int? pageSize, string? search,
                ILocationService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(await service.GetCountriesAsync(ct))
                    : Results.Ok(await service.GetCountriesPagedAsync(
                        new PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)))
            .RequireAuthorization();

        countries.MapPost("/", async (CountryWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var country = await service.CreateCountryAsync(dto, ct);
                return Results.Created($"/api/countries/{country.Id}", country);
            })
            .RequireAuthorization(Policies.StateAdmin);

        countries.MapPut("/{countryId:guid}",
                async (Guid countryId, CountryWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateCountryAsync(countryId, dto, ct)))
            .RequireAuthorization(Policies.StateAdmin);

        countries.MapDelete("/{countryId:guid}",
                async (Guid countryId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteCountryAsync(countryId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.StateAdmin);

        var originCities = api.MapGroup("/origin-cities").WithTags("Locations");

        originCities.MapGet("/", async (Guid? countryId, int? page, int? pageSize, string? search,
                ILocationService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(await service.GetOriginCitiesAsync(countryId, ct))
                    : Results.Ok(await service.GetOriginCitiesPagedAsync(
                        countryId, new PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)))
            .RequireAuthorization();

        originCities.MapPost("/", async (OriginCityWriteDto dto, ILocationService service, CancellationToken ct) =>
            {
                var city = await service.CreateOriginCityAsync(dto, ct);
                return Results.Created($"/api/origin-cities/{city.Id}", city);
            })
            .RequireAuthorization(Policies.StateAdmin);

        originCities.MapPut("/{cityId:guid}",
                async (Guid cityId, OriginCityWriteDto dto, ILocationService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateOriginCityAsync(cityId, dto, ct)))
            .RequireAuthorization(Policies.StateAdmin);

        originCities.MapDelete("/{cityId:guid}",
                async (Guid cityId, ILocationService service, CancellationToken ct) =>
                {
                    await service.DeleteOriginCityAsync(cityId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.StateAdmin);
    }
}
