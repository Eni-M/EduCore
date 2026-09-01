using EduCore.Data;
using EduCore.Dtos.Location;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class LocationRepository(AppDbContext context) : ILocationRepository
{
    public async Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct) =>
        await context.States
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new StateListDto(s.Id, s.Name, s.Cities.Count))
            .ToListAsync(ct);

    public async Task<State?> GetStateAsync(Guid stateId, CancellationToken ct) =>
        await context.States.FirstOrDefaultAsync(s => s.Id == stateId, ct);

    public async Task<bool> StateNameExistsAsync(string name, Guid? excludeStateId, CancellationToken ct) =>
        await context.States.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeStateId == null || s.Id != excludeStateId),
            ct);

    public async Task<bool> StateHasCitiesAsync(Guid stateId, CancellationToken ct) =>
        await context.Cities.AnyAsync(c => c.StateId == stateId, ct);

    public async Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct)
    {
        var query = context.Cities.AsNoTracking();

        if (stateId is not null)
            query = query.Where(c => c.StateId == stateId);

        return await query
            .OrderBy(c => c.State.Name)
            .ThenBy(c => c.Name)
            .Select(c => new CityListDto(c.Id, c.Name, c.StateId, c.State.Name))
            .ToListAsync(ct);
    }

    public async Task<City?> GetCityAsync(Guid cityId, CancellationToken ct) =>
        await context.Cities.FirstOrDefaultAsync(c => c.Id == cityId, ct);

    public async Task<CityListDto?> GetCityDtoAsync(Guid cityId, CancellationToken ct) =>
        await context.Cities
            .AsNoTracking()
            .Where(c => c.Id == cityId)
            .Select(c => new CityListDto(c.Id, c.Name, c.StateId, c.State.Name))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> CityNameExistsInStateAsync(
        Guid stateId, string name, Guid? excludeCityId, CancellationToken ct) =>
        await context.Cities.AnyAsync(
            c => c.StateId == stateId
                 && c.Name.ToLower() == name.ToLower()
                 && (excludeCityId == null || c.Id != excludeCityId),
            ct);

    public async Task<bool> CityIsInUseAsync(Guid cityId, CancellationToken ct) =>
        await context.Students.IgnoreQueryFilters().AnyAsync(s => s.CityId == cityId, ct)
        || await context.Schools.AnyAsync(s => s.CityId == cityId, ct);

    public async Task<bool> CityHasZonesAsync(Guid cityId, CancellationToken ct) =>
        await context.Zones.AnyAsync(z => z.CityId == cityId, ct);

    public async Task<IEnumerable<ZoneListDto>> GetZonesAsync(Guid? cityId, CancellationToken ct)
    {
        var query = context.Zones.AsNoTracking().AsQueryable();

        if (cityId is not null)
            query = query.Where(z => z.CityId == cityId);

        return await query
            .OrderBy(z => z.City.State.Name)
            .ThenBy(z => z.City.Name)
            .ThenBy(z => z.Name)
            .Select(z => new ZoneListDto(z.Id, z.Name, z.CityId, z.City.Name, z.City.State.Name))
            .ToListAsync(ct);
    }

    public async Task<Zone?> GetZoneAsync(Guid zoneId, CancellationToken ct) =>
        await context.Zones.FirstOrDefaultAsync(z => z.Id == zoneId, ct);

    public async Task<ZoneListDto?> GetZoneDtoAsync(Guid zoneId, CancellationToken ct) =>
        await context.Zones
            .AsNoTracking()
            .Where(z => z.Id == zoneId)
            .Select(z => new ZoneListDto(z.Id, z.Name, z.CityId, z.City.Name, z.City.State.Name))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> ZoneNameExistsInCityAsync(
        Guid cityId, string name, Guid? excludeZoneId, CancellationToken ct) =>
        await context.Zones.AnyAsync(
            z => z.CityId == cityId
                 && z.Name.ToLower() == name.ToLower()
                 && (excludeZoneId == null || z.Id != excludeZoneId),
            ct);

    public async Task<bool> ZoneIsInUseAsync(Guid zoneId, CancellationToken ct) =>
        await context.Schools.AnyAsync(s => s.ZoneId == zoneId, ct);

    public async Task AddStateAsync(State state) => await context.States.AddAsync(state);
    public async Task AddCityAsync(City city) => await context.Cities.AddAsync(city);
    public async Task AddZoneAsync(Zone zone) => await context.Zones.AddAsync(zone);
    public void RemoveState(State state) => context.States.Remove(state);
    public void RemoveCity(City city) => context.Cities.Remove(city);
    public void RemoveZone(Zone zone) => context.Zones.Remove(zone);

    public async Task<bool> SaveChangesAsync(CancellationToken ct) =>
        await context.SaveChangesAsync(ct) > 0;
}
