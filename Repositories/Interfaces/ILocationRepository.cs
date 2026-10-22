using EduCore.Dtos.Location;
using EduCore.Dtos.Pagination;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ILocationRepository
{
    Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct);
    Task<State?> GetStateAsync(Guid stateId, CancellationToken ct);
    Task<bool> StateNameExistsAsync(string name, Guid? excludeStateId, CancellationToken ct);
    Task<bool> StateHasCitiesAsync(Guid stateId, CancellationToken ct);

    Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct);
    Task<City?> GetCityAsync(Guid cityId, CancellationToken ct);
    Task<CityListDto?> GetCityDtoAsync(Guid cityId, CancellationToken ct);

    Task<bool> CityNameExistsInStateAsync(
        Guid stateId, string name, Guid? excludeCityId, CancellationToken ct);

    Task<bool> CityIsInUseAsync(Guid cityId, CancellationToken ct);
    Task<bool> CityHasZonesAsync(Guid cityId, CancellationToken ct);

    Task<IEnumerable<ZoneListDto>> GetZonesAsync(Guid? cityId, CancellationToken ct);
    Task<Zone?> GetZoneAsync(Guid zoneId, CancellationToken ct);
    Task<ZoneListDto?> GetZoneDtoAsync(Guid zoneId, CancellationToken ct);

    Task<bool> ZoneNameExistsInCityAsync(Guid cityId, string name, Guid? excludeZoneId, CancellationToken ct);
    Task<bool> ZoneIsInUseAsync(Guid zoneId, CancellationToken ct);

    Task<IEnumerable<CountryListDto>> GetCountriesAsync(CancellationToken ct);
    Task<Country?> GetCountryAsync(Guid countryId, CancellationToken ct);
    Task<bool> CountryNameExistsAsync(string name, Guid? excludeCountryId, CancellationToken ct);
    Task<bool> CountryIsInUseAsync(Guid countryId, CancellationToken ct);

    Task<IEnumerable<OriginCityListDto>> GetOriginCitiesAsync(Guid? countryId, CancellationToken ct);
    Task<PagedResultDto<OriginCityListDto>> GetOriginCitiesPagedAsync(
        Guid? countryId, PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<OriginCity?> GetOriginCityAsync(Guid cityId, CancellationToken ct);
    Task<OriginCityListDto?> GetOriginCityDtoAsync(Guid cityId, CancellationToken ct);
    Task<bool> OriginCityNameExistsInCountryAsync(
        Guid countryId, string name, Guid? excludeCityId, CancellationToken ct);
    Task<bool> OriginCityIsInUseAsync(Guid cityId, CancellationToken ct);

    Task AddStateAsync(State state);
    Task AddCityAsync(City city);
    Task AddZoneAsync(Zone zone);
    Task AddCountryAsync(Country country);
    Task AddOriginCityAsync(OriginCity city);
    void RemoveState(State state);
    void RemoveCity(City city);
    void RemoveZone(Zone zone);
    void RemoveCountry(Country country);
    void RemoveOriginCity(OriginCity city);
    Task<bool> SaveChangesAsync(CancellationToken ct);
}
