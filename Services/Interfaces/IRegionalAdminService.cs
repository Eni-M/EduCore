using EduCore.Dtos.RegionalAdmin;

namespace EduCore.Services.Interfaces;

public interface IRegionalAdminService
{
    Task<IEnumerable<RegionalAdminListDto>> GetAllAsync(CancellationToken ct);
    Task<RegionalAdminListDto> GetByIdAsync(Guid regionalAdminId, CancellationToken ct);
    Task<RegionalAdminListDto> CreateAsync(RegionalAdminCreateDto dto, CancellationToken ct);
    Task<RegionalAdminListDto> UpdateAsync(Guid regionalAdminId, RegionalAdminEditDto dto, CancellationToken ct);
}
