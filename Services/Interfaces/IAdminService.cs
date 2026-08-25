using EduCore.Dtos.Admin;

namespace EduCore.Services.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<AdminListDto>> GetAllAsync(Guid? schoolId, CancellationToken ct);
    Task<AdminListDto> GetByIdAsync(Guid adminId, CancellationToken ct);
    Task<AdminListDto> CreateAsync(AdminCreateDto dto, CancellationToken ct);
    Task<AdminListDto> UpdateAsync(Guid adminId, AdminEditDto dto, CancellationToken ct);
}
