using EduCore.Dtos.Location;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;

namespace EduCore.Services;

public class LocationService(ILocationRepository locations) : ILocationService
{
    public async Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct) =>
        await locations.GetStatesAsync(ct);

    public async Task<PagedResultDto<StateListDto>> GetStatesPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetStatesAsync(ct);
        return all.Search(search, s => new[] { s.Name }).ToPagedResult(pagination);
    }

    public async Task<StateListDto> CreateStateAsync(StateWriteDto dto, CancellationToken ct)
    {
        if (await locations.StateNameExistsAsync(dto.Name, null, ct))
            throw new ConflictException($"A state named {dto.Name} already exists.");

        var state = new State { Name = dto.Name.Trim() };

        await locations.AddStateAsync(state);
        await locations.SaveChangesAsync(ct);

        return new StateListDto(state.Id, state.Name, 0);
    }

    public async Task<StateListDto> UpdateStateAsync(Guid stateId, StateWriteDto dto, CancellationToken ct)
    {
        var state = await locations.GetStateAsync(stateId, ct)
                    ?? throw new NotFoundException("State", stateId);

        if (await locations.StateNameExistsAsync(dto.Name, stateId, ct))
            throw new ConflictException($"A state named {dto.Name} already exists.");

        state.Name = dto.Name.Trim();
        await locations.SaveChangesAsync(ct);

        return (await locations.GetStatesAsync(ct)).First(s => s.Id == stateId);
    }

    public async Task DeleteStateAsync(Guid stateId, CancellationToken ct)
    {
        var state = await locations.GetStateAsync(stateId, ct)
                    ?? throw new NotFoundException("State", stateId);

        if (await locations.StateHasCitiesAsync(stateId, ct))
            throw new ConflictException(
                "This state still has cities. Move or delete them first.");

        locations.RemoveState(state);
        await locations.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct) =>
        await locations.GetCitiesAsync(stateId, ct);

    public async Task<PagedResultDto<CityListDto>> GetCitiesPagedAsync(
        Guid? stateId, PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetCitiesAsync(stateId, ct);
        return all.Search(search, c => new[] { c.Name, c.StateName }).ToPagedResult(pagination);
    }

    public async Task<CityListDto> CreateCityAsync(CityWriteDto dto, CancellationToken ct)
    {
        _ = await locations.GetStateAsync(dto.StateId, ct)
            ?? throw new NotFoundException("State", dto.StateId);

        if (await locations.CityNameExistsInStateAsync(dto.StateId, dto.Name, null, ct))
            throw new ConflictException($"A city named {dto.Name} already exists in that state.");

        var city = new City { Name = dto.Name.Trim(), StateId = dto.StateId };

        await locations.AddCityAsync(city);
        await locations.SaveChangesAsync(ct);

        return await locations.GetCityDtoAsync(city.Id, ct)
               ?? throw new NotFoundException("City", city.Id);
    }

    public async Task<CityListDto> UpdateCityAsync(Guid cityId, CityWriteDto dto, CancellationToken ct)
    {
        var city = await locations.GetCityAsync(cityId, ct)
                   ?? throw new NotFoundException("City", cityId);

        _ = await locations.GetStateAsync(dto.StateId, ct)
            ?? throw new NotFoundException("State", dto.StateId);

        if (await locations.CityNameExistsInStateAsync(dto.StateId, dto.Name, cityId, ct))
            throw new ConflictException($"A city named {dto.Name} already exists in that state.");

        city.Name = dto.Name.Trim();
        city.StateId = dto.StateId;
        await locations.SaveChangesAsync(ct);

        return await locations.GetCityDtoAsync(cityId, ct)
               ?? throw new NotFoundException("City", cityId);
    }

    public async Task DeleteCityAsync(Guid cityId, CancellationToken ct)
    {
        var city = await locations.GetCityAsync(cityId, ct)
                   ?? throw new NotFoundException("City", cityId);

        if (await locations.CityHasZonesAsync(cityId, ct))
            throw new ConflictException("This city still has zones. Move or delete them first.");

        if (await locations.CityIsInUseAsync(cityId, ct))
            throw new ConflictException(
                "This city is recorded against a student or a school and cannot be deleted.");

        locations.RemoveCity(city);
        await locations.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<ZoneListDto>> GetZonesAsync(Guid? cityId, CancellationToken ct) =>
        await locations.GetZonesAsync(cityId, ct);

    public async Task<PagedResultDto<ZoneListDto>> GetZonesPagedAsync(
        Guid? cityId, PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetZonesAsync(cityId, ct);
        return all.Search(search, z => new[] { z.Name, z.CityName, z.StateName }).ToPagedResult(pagination);
    }

    public async Task<ZoneListDto> CreateZoneAsync(ZoneWriteDto dto, CancellationToken ct)
    {
        _ = await locations.GetCityAsync(dto.CityId, ct)
            ?? throw new NotFoundException("City", dto.CityId);

        if (await locations.ZoneNameExistsInCityAsync(dto.CityId, dto.Name, null, ct))
            throw new ConflictException($"A zone named {dto.Name} already exists in that city.");

        var zone = new Zone { Name = dto.Name.Trim(), CityId = dto.CityId };

        await locations.AddZoneAsync(zone);
        await locations.SaveChangesAsync(ct);

        return await locations.GetZoneDtoAsync(zone.Id, ct)
               ?? throw new NotFoundException("Zone", zone.Id);
    }

    public async Task<ZoneListDto> UpdateZoneAsync(Guid zoneId, ZoneWriteDto dto, CancellationToken ct)
    {
        var zone = await locations.GetZoneAsync(zoneId, ct)
                   ?? throw new NotFoundException("Zone", zoneId);

        _ = await locations.GetCityAsync(dto.CityId, ct)
            ?? throw new NotFoundException("City", dto.CityId);

        if (await locations.ZoneNameExistsInCityAsync(dto.CityId, dto.Name, zoneId, ct))
            throw new ConflictException($"A zone named {dto.Name} already exists in that city.");

        zone.Name = dto.Name.Trim();
        zone.CityId = dto.CityId;
        await locations.SaveChangesAsync(ct);

        return await locations.GetZoneDtoAsync(zoneId, ct)
               ?? throw new NotFoundException("Zone", zoneId);
    }

    public async Task DeleteZoneAsync(Guid zoneId, CancellationToken ct)
    {
        var zone = await locations.GetZoneAsync(zoneId, ct)
                   ?? throw new NotFoundException("Zone", zoneId);

        if (await locations.ZoneIsInUseAsync(zoneId, ct))
            throw new ConflictException("This zone is recorded against a school and cannot be deleted.");

        locations.RemoveZone(zone);
        await locations.SaveChangesAsync(ct);
    }

    // --- Student origin: countries and their cities ---

    public async Task<IEnumerable<CountryListDto>> GetCountriesAsync(CancellationToken ct) =>
        await locations.GetCountriesAsync(ct);

    public async Task<PagedResultDto<CountryListDto>> GetCountriesPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetCountriesAsync(ct);
        return all.Search(search, c => new[] { c.Name }).ToPagedResult(pagination);
    }

    public async Task<CountryListDto> CreateCountryAsync(CountryWriteDto dto, CancellationToken ct)
    {
        if (await locations.CountryNameExistsAsync(dto.Name, null, ct))
            throw new ConflictException($"A country named {dto.Name} already exists.");

        var country = new Country { Name = dto.Name.Trim() };

        await locations.AddCountryAsync(country);
        await locations.SaveChangesAsync(ct);

        return new CountryListDto(country.Id, country.Name, 0);
    }

    public async Task<CountryListDto> UpdateCountryAsync(Guid countryId, CountryWriteDto dto, CancellationToken ct)
    {
        var country = await locations.GetCountryAsync(countryId, ct)
                      ?? throw new NotFoundException("Country", countryId);

        if (await locations.CountryNameExistsAsync(dto.Name, countryId, ct))
            throw new ConflictException($"A country named {dto.Name} already exists.");

        country.Name = dto.Name.Trim();
        await locations.SaveChangesAsync(ct);

        return (await locations.GetCountriesAsync(ct)).First(c => c.Id == countryId);
    }

    public async Task DeleteCountryAsync(Guid countryId, CancellationToken ct)
    {
        var country = await locations.GetCountryAsync(countryId, ct)
                      ?? throw new NotFoundException("Country", countryId);

        if (await locations.CountryIsInUseAsync(countryId, ct))
            throw new ConflictException(
                "This country still has cities or is recorded against a student, and cannot be deleted.");

        locations.RemoveCountry(country);
        await locations.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<OriginCityListDto>> GetOriginCitiesAsync(Guid? countryId, CancellationToken ct) =>
        await locations.GetOriginCitiesAsync(countryId, ct);

    public async Task<PagedResultDto<OriginCityListDto>> GetOriginCitiesPagedAsync(
        Guid? countryId, PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetOriginCitiesAsync(countryId, ct);
        return all.Search(search, c => new[] { c.Name, c.CountryName }).ToPagedResult(pagination);
    }

    public async Task<OriginCityListDto> CreateOriginCityAsync(OriginCityWriteDto dto, CancellationToken ct)
    {
        _ = await locations.GetCountryAsync(dto.CountryId, ct)
            ?? throw new NotFoundException("Country", dto.CountryId);

        if (await locations.OriginCityNameExistsInCountryAsync(dto.CountryId, dto.Name, null, ct))
            throw new ConflictException($"A city named {dto.Name} already exists in that country.");

        var city = new OriginCity { Name = dto.Name.Trim(), CountryId = dto.CountryId };

        await locations.AddOriginCityAsync(city);
        await locations.SaveChangesAsync(ct);

        return await locations.GetOriginCityDtoAsync(city.Id, ct)
               ?? throw new NotFoundException("City", city.Id);
    }

    public async Task<OriginCityListDto> UpdateOriginCityAsync(
        Guid cityId, OriginCityWriteDto dto, CancellationToken ct)
    {
        var city = await locations.GetOriginCityAsync(cityId, ct)
                   ?? throw new NotFoundException("City", cityId);

        _ = await locations.GetCountryAsync(dto.CountryId, ct)
            ?? throw new NotFoundException("Country", dto.CountryId);

        if (await locations.OriginCityNameExistsInCountryAsync(dto.CountryId, dto.Name, cityId, ct))
            throw new ConflictException($"A city named {dto.Name} already exists in that country.");

        city.Name = dto.Name.Trim();
        city.CountryId = dto.CountryId;
        await locations.SaveChangesAsync(ct);

        return await locations.GetOriginCityDtoAsync(cityId, ct)
               ?? throw new NotFoundException("City", cityId);
    }

    public async Task DeleteOriginCityAsync(Guid cityId, CancellationToken ct)
    {
        var city = await locations.GetOriginCityAsync(cityId, ct)
                   ?? throw new NotFoundException("City", cityId);

        if (await locations.OriginCityIsInUseAsync(cityId, ct))
            throw new ConflictException(
                "This city is recorded against a student and cannot be deleted.");

        locations.RemoveOriginCity(city);
        await locations.SaveChangesAsync(ct);
    }
}
