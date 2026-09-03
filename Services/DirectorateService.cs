using AutoMapper;
using EduCore.Dtos.Directorate;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;

namespace EduCore.Services;

public class DirectorateService(
    IDirectorateRepository directorates,
    ISchoolRepository schools,
    IUserRepository userRepository,
    IUserService users,
    ICurrentUser currentUser,
    IMapper mapper) : IDirectorateService
{
    public async Task<IEnumerable<DirectorateListDto>> GetAllAsync(CancellationToken ct) =>
        mapper.Map<IEnumerable<DirectorateListDto>>(
            await directorates.GetByCityAsync(currentUser.RequireCityId(), ct));

    public async Task<PagedResultDto<DirectorateListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetAllAsync(ct);
        return all.Search(search, d => new[] { d.FirstName, d.LastName, d.Email, d.SchoolName })
            .ToPagedResult(pagination);
    }

    public async Task<DirectorateListDto> GetByIdAsync(Guid directorateId, CancellationToken ct)
    {
        var directorate = await directorates.GetWithUserAsync(directorateId, ct)
                          ?? throw new NotFoundException("Directorate", directorateId);

        EnsureInOwnCity(directorate.School);

        return mapper.Map<DirectorateListDto>(directorate);
    }

    public async Task<DirectorateListDto> CreateAsync(DirectorateCreateDto dto, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(dto.SchoolId, ct)
                     ?? throw new NotFoundException("School", dto.SchoolId);

        EnsureInOwnCity(school);

        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.Directorate, ct);

        var directorate = new Directorate { User = user, SchoolId = dto.SchoolId };

        await directorates.AddAsync(directorate);
        await directorates.SaveChangesAsync(ct);

        return await GetByIdAsync(directorate.Id, ct);
    }

    public async Task<DirectorateListDto> UpdateAsync(Guid directorateId, DirectorateEditDto dto, CancellationToken ct)
    {
        var directorate = await directorates.GetTrackedAsync(directorateId, ct)
                          ?? throw new NotFoundException("Directorate", directorateId);

        var currentSchool = await schools.GetByIdAsync(directorate.SchoolId, ct)
                            ?? throw new NotFoundException("School", directorate.SchoolId);
        EnsureInOwnCity(currentSchool);

        var targetSchool = await schools.GetByIdAsync(dto.SchoolId, ct)
                           ?? throw new NotFoundException("School", dto.SchoolId);
        EnsureInOwnCity(targetSchool);

        var user = await userRepository.GetByIdAsync(directorate.UserId, ct)
                   ?? throw new NotFoundException("User", directorate.UserId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        directorate.SchoolId = dto.SchoolId;
        directorates.Update(directorate);
        await directorates.SaveChangesAsync(ct);

        return await GetByIdAsync(directorateId, ct);
    }

    private void EnsureInOwnCity(School school)
    {
        if (school.CityId is null || school.CityId != currentUser.RequireCityId())
            throw new ForbiddenException("You can only manage directorate accounts for schools in your city.");
    }
}
