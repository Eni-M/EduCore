using EduCore.Dtos.Pagination;
using EduCore.Dtos.StateAdmin;

namespace EduCore.Services.Interfaces;

public interface IStateAdminService
{
    Task<IEnumerable<StateAdminListDto>> GetAllAsync(CancellationToken ct);
    Task<PagedResultDto<StateAdminListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<StateAdminListDto> GetByIdAsync(Guid stateAdminId, CancellationToken ct);
    Task<StateAdminListDto> CreateAsync(StateAdminCreateDto dto, CancellationToken ct);
    Task<StateAdminListDto> UpdateAsync(Guid stateAdminId, StateAdminEditDto dto, CancellationToken ct);
}
