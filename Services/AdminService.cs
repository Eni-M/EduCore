using AutoMapper;
using EduCore.Dtos.Admin;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class AdminService(
    IAdminRepository admins,
    ISchoolRepository schools,
    IUserRepository userRepository,
    IUserService users,
    IMapper mapper) : IAdminService
{
    public async Task<IEnumerable<AdminListDto>> GetAllAsync(Guid? schoolId, CancellationToken ct) =>
        mapper.Map<IEnumerable<AdminListDto>>(await admins.GetAllAcrossSchoolsAsync(schoolId, ct));

    public async Task<AdminListDto> GetByIdAsync(Guid adminId, CancellationToken ct)
    {
        var admin = await admins.GetWithUserAsync(adminId, ct)
                    ?? throw new NotFoundException("Admin", adminId);

        return mapper.Map<AdminListDto>(admin);
    }

    public async Task<AdminListDto> CreateAsync(AdminCreateDto dto, CancellationToken ct)
    {
        _ = await schools.GetByIdAsync(dto.SchoolId, ct)
            ?? throw new NotFoundException("School", dto.SchoolId);

        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.Admin, ct);

        var admin = new Admin { User = user, SchoolId = dto.SchoolId };

        await admins.AddAsync(admin);
        await admins.SaveChangesAsync(ct);

        return await GetByIdAsync(admin.Id, ct);
    }

    public async Task<AdminListDto> UpdateAsync(Guid adminId, AdminEditDto dto, CancellationToken ct)
    {
        var admin = await admins.GetTrackedAsync(adminId, ct)
                    ?? throw new NotFoundException("Admin", adminId);

        _ = await schools.GetByIdAsync(dto.SchoolId, ct)
            ?? throw new NotFoundException("School", dto.SchoolId);

        var user = await userRepository.GetByIdAsync(admin.UserId, ct)
                   ?? throw new NotFoundException("User", admin.UserId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        admin.SchoolId = dto.SchoolId;
        admins.Update(admin);
        await admins.SaveChangesAsync(ct);

        return await GetByIdAsync(adminId, ct);
    }
}
