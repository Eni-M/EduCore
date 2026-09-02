using AutoMapper;
using EduCore.Dtos.School;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class SchoolService(
    ISchoolRepository schools,
    ILocationRepository locations,
    ICurrentUser currentUser,
    IMapper mapper) : ISchoolService
{
    public async Task<IEnumerable<SchoolListDto>> GetAllAsync(bool activeOnly, CancellationToken ct)
    {
        if (currentUser.IsRegionalAdmin)
            return mapper.Map<IEnumerable<SchoolListDto>>(
                await schools.GetByCityAsync(currentUser.RequireCityId(), activeOnly, ct));

        if (currentUser.IsStateAdmin)
            return mapper.Map<IEnumerable<SchoolListDto>>(
                await schools.GetByStateAsync(currentUser.RequireStateId(), activeOnly, ct));

        return mapper.Map<IEnumerable<SchoolListDto>>(await schools.GetAllOrderedAsync(activeOnly, ct));
    }

    public async Task<SchoolListDto> GetByIdAsync(Guid schoolId, CancellationToken ct)
    {
        var school = await schools.GetWithLocationAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);

        await EnsureInCallerScopeAsync(school.CityId, ct);

        return mapper.Map<SchoolListDto>(school);
    }

    public async Task<SchoolListDto> CreateAsync(SchoolCreateDto dto, CancellationToken ct)
    {
        if (await schools.NameExistsAsync(dto.Name, null, ct))
            throw new ConflictException($"A school named {dto.Name} already exists.");

        var cityId = await ResolveCreateCityAsync(dto.CityId, ct);

        await ValidateLocationAsync(cityId, dto.ZoneId, ct);

        var school = mapper.Map<School>(dto);
        school.CityId = cityId;

        await schools.AddAsync(school);
        await schools.SaveChangesAsync(ct);

        return await GetByIdAsync(school.Id, ct);
    }

    public async Task<SchoolListDto> UpdateAsync(Guid schoolId, SchoolEditDto dto, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);

        await EnsureInCallerScopeAsync(school.CityId, ct);

        if (await schools.NameExistsAsync(dto.Name, schoolId, ct))
            throw new ConflictException($"A school named {dto.Name} already exists.");

        var cityId = await ResolveCreateCityAsync(dto.CityId, ct);

        await ValidateLocationAsync(cityId, dto.ZoneId, ct);

        mapper.Map(dto, school);
        school.CityId = cityId;

        schools.Update(school);
        await schools.SaveChangesAsync(ct);

        return await GetByIdAsync(schoolId, ct);
    }

    public async Task SetActiveAsync(Guid schoolId, bool isActive, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);

        await EnsureInCallerScopeAsync(school.CityId, ct);

        school.DeactivatedAt = isActive ? null : DateTime.UtcNow;

        schools.Update(school);
        await schools.SaveChangesAsync(ct);
    }

    private async Task<Guid?> ResolveCreateCityAsync(Guid? requestedCityId, CancellationToken ct)
    {
        if (currentUser.IsRegionalAdmin)
            return currentUser.RequireCityId();

        if (currentUser.IsStateAdmin)
        {
            if (requestedCityId is not { } cityId)
                throw new BusinessException("A city is required when creating a school.");

            var city = await locations.GetCityAsync(cityId, ct)
                       ?? throw new NotFoundException("City", cityId);

            if (city.StateId != currentUser.RequireStateId())
                throw new ForbiddenException("You can only place schools in cities within your state.");

            return cityId;
        }

        return requestedCityId;
    }

    private async Task EnsureInCallerScopeAsync(Guid? schoolCityId, CancellationToken ct)
    {
        if (currentUser.IsRegionalAdmin)
        {
            if (schoolCityId != currentUser.RequireCityId())
                throw new ForbiddenException("This school is not in your city.");

            return;
        }

        if (currentUser.IsStateAdmin)
        {
            if (schoolCityId is not { } cityId)
                throw new ForbiddenException("This school is not in your state.");

            var city = await locations.GetCityAsync(cityId, ct);
            if (city?.StateId != currentUser.RequireStateId())
                throw new ForbiddenException("This school is not in your state.");
        }
    }

    private async Task ValidateLocationAsync(Guid? cityId, Guid? zoneId, CancellationToken ct)
    {
        if (cityId is not null && await locations.GetCityAsync(cityId.Value, ct) is null)
            throw new NotFoundException("City", cityId.Value);

        if (zoneId is not null)
        {
            var zone = await locations.GetZoneAsync(zoneId.Value, ct)
                       ?? throw new NotFoundException("Zone", zoneId.Value);

            if (cityId is null || zone.CityId != cityId.Value)
                throw new BusinessException("The zone must belong to the selected city.");
        }
    }
}
