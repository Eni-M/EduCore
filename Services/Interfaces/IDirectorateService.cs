using EduCore.Dtos.Directorate;
using EduCore.Dtos.Pagination;

namespace EduCore.Services.Interfaces;

public interface IDirectorateService
{
    Task<IEnumerable<DirectorateListDto>> GetAllAsync(CancellationToken ct);
    Task<PagedResultDto<DirectorateListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<DirectorateListDto> GetByIdAsync(Guid directorateId, CancellationToken ct);
    Task<DirectorateListDto> CreateAsync(DirectorateCreateDto dto, CancellationToken ct);
    Task<DirectorateListDto> UpdateAsync(Guid directorateId, DirectorateEditDto dto, CancellationToken ct);
}
