using EduCore.Dtos.School;

namespace EduCore.Services.Interfaces;

public interface ISchoolService
{
    Task<IEnumerable<SchoolListDto>> GetAllAsync(CancellationToken ct);
    Task<SchoolListDto> GetByIdAsync(Guid schoolId, CancellationToken ct);
    Task<SchoolListDto> CreateAsync(SchoolCreateDto dto, CancellationToken ct);
    Task<SchoolListDto> UpdateAsync(Guid schoolId, SchoolEditDto dto, CancellationToken ct);

    Task<IEnumerable<SchoolStatisticsDto>> GetStatisticsAsync(CancellationToken ct);
}
