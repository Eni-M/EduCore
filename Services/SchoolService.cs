using AutoMapper;
using EduCore.Dtos.School;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class SchoolService(ISchoolRepository schools, ILocationRepository locations, IMapper mapper) : ISchoolService
{
    public async Task<IEnumerable<SchoolListDto>> GetAllAsync(bool activeOnly, CancellationToken ct) =>
        mapper.Map<IEnumerable<SchoolListDto>>(await schools.GetAllOrderedAsync(activeOnly, ct));

    public async Task<SchoolListDto> GetByIdAsync(Guid schoolId, CancellationToken ct)
    {
        var school = await schools.GetWithLocationAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);

        return mapper.Map<SchoolListDto>(school);
    }

    public async Task<SchoolListDto> CreateAsync(SchoolCreateDto dto, CancellationToken ct)
    {
        if (await schools.NameExistsAsync(dto.Name, null, ct))
            throw new ConflictException($"A school named {dto.Name} already exists.");

        await ValidateLocationAsync(dto.CityId, dto.ZoneId, ct);

        var school = mapper.Map<School>(dto);

        await schools.AddAsync(school);
        await schools.SaveChangesAsync(ct);

        return await GetByIdAsync(school.Id, ct);
    }

    public async Task<SchoolListDto> UpdateAsync(Guid schoolId, SchoolEditDto dto, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);

        if (await schools.NameExistsAsync(dto.Name, schoolId, ct))
            throw new ConflictException($"A school named {dto.Name} already exists.");

        await ValidateLocationAsync(dto.CityId, dto.ZoneId, ct);

        mapper.Map(dto, school);

        schools.Update(school);
        await schools.SaveChangesAsync(ct);

        return await GetByIdAsync(schoolId, ct);
    }

    public async Task SetActiveAsync(Guid schoolId, bool isActive, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);

        school.DeactivatedAt = isActive ? null : DateTime.UtcNow;

        schools.Update(school);
        await schools.SaveChangesAsync(ct);
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
