using EduCore.Dtos.Location;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

// States and cities are one repository rather than two: they are never used apart, and every
// city query needs its state alongside it.
public interface ILocationRepository
{
    Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct);
    Task<State?> GetStateAsync(Guid stateId, CancellationToken ct);
    Task<bool> StateNameExistsAsync(string name, Guid? excludeStateId, CancellationToken ct);
    Task<bool> StateHasCitiesAsync(Guid stateId, CancellationToken ct);

    Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct);
    Task<City?> GetCityAsync(Guid cityId, CancellationToken ct);
    Task<CityListDto?> GetCityDtoAsync(Guid cityId, CancellationToken ct);

    // Scoped to the state: two different states may each have a "Springfield", and refusing
    // that would be wrong.
    Task<bool> CityNameExistsInStateAsync(
        Guid stateId, string name, Guid? excludeCityId, CancellationToken ct);

    Task<bool> CityIsInUseAsync(Guid cityId, CancellationToken ct);

    Task AddStateAsync(State state);
    Task AddCityAsync(City city);
    void RemoveState(State state);
    void RemoveCity(City city);
    Task<bool> SaveChangesAsync(CancellationToken ct);
}
