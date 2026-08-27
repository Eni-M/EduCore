using EduCore.Dtos.Location;

namespace EduCore.Services.Interfaces;

public interface ILocationService
{
    Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct);
    Task<StateListDto> CreateStateAsync(StateWriteDto dto, CancellationToken ct);
    Task<StateListDto> UpdateStateAsync(Guid stateId, StateWriteDto dto, CancellationToken ct);
    Task DeleteStateAsync(Guid stateId, CancellationToken ct);

    Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct);
    Task<CityListDto> CreateCityAsync(CityWriteDto dto, CancellationToken ct);
    Task<CityListDto> UpdateCityAsync(Guid cityId, CityWriteDto dto, CancellationToken ct);
    Task DeleteCityAsync(Guid cityId, CancellationToken ct);
}
