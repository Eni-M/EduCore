using EduCore.Dtos.School;
using EduCore.Dtos.Pagination;

namespace EduCore.Services.Interfaces;

public interface ISchoolService
{
    Task<IEnumerable<SchoolListDto>> GetAllAsync(bool activeOnly, CancellationToken ct);
    Task<PagedResultDto<SchoolListDto>> GetPagedAsync(
        bool activeOnly, PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<SchoolListDto> GetByIdAsync(Guid schoolId, CancellationToken ct);
    Task<SchoolListDto> CreateAsync(SchoolCreateDto dto, CancellationToken ct);
    Task<SchoolListDto> UpdateAsync(Guid schoolId, SchoolEditDto dto, CancellationToken ct);

    Task SetActiveAsync(Guid schoolId, bool isActive, CancellationToken ct);
}
