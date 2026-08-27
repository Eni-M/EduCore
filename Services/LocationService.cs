using EduCore.Dtos.Location;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

// Places are reference data: everything else points at them, so the rules here are about
// keeping that reference sound -- no duplicate names, and nothing deleted out from under a
// record that still uses it.
public class LocationService(ILocationRepository locations) : ILocationService
{
    public async Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct) =>
        await locations.GetStatesAsync(ct);

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

        // Renaming in place is the point of holding these as rows: every student and school
        // pointing here follows the correction automatically.
        state.Name = dto.Name.Trim();
        await locations.SaveChangesAsync(ct);

        return (await locations.GetStatesAsync(ct)).First(s => s.Id == stateId);
    }

    public async Task DeleteStateAsync(Guid stateId, CancellationToken ct)
    {
        var state = await locations.GetStateAsync(stateId, ct)
                    ?? throw new NotFoundException("State", stateId);

        // Refused rather than cascaded: deleting a state silently taking its cities with it
        // would strand every student who lives in one.
        if (await locations.StateHasCitiesAsync(stateId, ct))
            throw new ConflictException(
                "This state still has cities. Move or delete them first.");

        locations.RemoveState(state);
        await locations.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct) =>
        await locations.GetCitiesAsync(stateId, ct);

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

        if (await locations.CityIsInUseAsync(cityId, ct))
            throw new ConflictException(
                "This city is recorded against a student or a school and cannot be deleted.");

        locations.RemoveCity(city);
        await locations.SaveChangesAsync(ct);
    }
}
