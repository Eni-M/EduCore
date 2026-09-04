using EduCore.Dtos.Location;
using EduCore.Dtos.Pagination;

namespace EduCore.Services.Interfaces;

public interface ILocationService
{
    Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct);
    Task<PagedResultDto<StateListDto>> GetStatesPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<StateListDto> CreateStateAsync(StateWriteDto dto, CancellationToken ct);
    Task<StateListDto> UpdateStateAsync(Guid stateId, StateWriteDto dto, CancellationToken ct);
    Task DeleteStateAsync(Guid stateId, CancellationToken ct);

    Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct);
    Task<PagedResultDto<CityListDto>> GetCitiesPagedAsync(
        Guid? stateId, PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<CityListDto> CreateCityAsync(CityWriteDto dto, CancellationToken ct);
    Task<CityListDto> UpdateCityAsync(Guid cityId, CityWriteDto dto, CancellationToken ct);
    Task DeleteCityAsync(Guid cityId, CancellationToken ct);

    Task<IEnumerable<ZoneListDto>> GetZonesAsync(Guid? cityId, CancellationToken ct);
    Task<PagedResultDto<ZoneListDto>> GetZonesPagedAsync(
        Guid? cityId, PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<ZoneListDto> CreateZoneAsync(ZoneWriteDto dto, CancellationToken ct);
    Task<ZoneListDto> UpdateZoneAsync(Guid zoneId, ZoneWriteDto dto, CancellationToken ct);
    Task DeleteZoneAsync(Guid zoneId, CancellationToken ct);

    Task<IEnumerable<CountryListDto>> GetCountriesAsync(CancellationToken ct);
    Task<PagedResultDto<CountryListDto>> GetCountriesPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<CountryListDto> CreateCountryAsync(CountryWriteDto dto, CancellationToken ct);
    Task<CountryListDto> UpdateCountryAsync(Guid countryId, CountryWriteDto dto, CancellationToken ct);
    Task DeleteCountryAsync(Guid countryId, CancellationToken ct);

    Task<IEnumerable<OriginCityListDto>> GetOriginCitiesAsync(Guid? countryId, CancellationToken ct);
    Task<PagedResultDto<OriginCityListDto>> GetOriginCitiesPagedAsync(
        Guid? countryId, PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<OriginCityListDto> CreateOriginCityAsync(OriginCityWriteDto dto, CancellationToken ct);
    Task<OriginCityListDto> UpdateOriginCityAsync(Guid cityId, OriginCityWriteDto dto, CancellationToken ct);
    Task DeleteOriginCityAsync(Guid cityId, CancellationToken ct);
}
