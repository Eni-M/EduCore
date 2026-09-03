using EduCore.Dtos.RegionalAdmin;
using EduCore.Dtos.Pagination;

namespace EduCore.Services.Interfaces;

public interface IRegionalAdminService
{
    Task<IEnumerable<RegionalAdminListDto>> GetAllAsync(CancellationToken ct);
    Task<PagedResultDto<RegionalAdminListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<RegionalAdminListDto> GetByIdAsync(Guid regionalAdminId, CancellationToken ct);
    Task<RegionalAdminListDto> CreateAsync(RegionalAdminCreateDto dto, CancellationToken ct);
    Task<RegionalAdminListDto> UpdateAsync(Guid regionalAdminId, RegionalAdminEditDto dto, CancellationToken ct);
}
