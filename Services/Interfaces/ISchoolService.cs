using EduCore.Dtos.School;

namespace EduCore.Services.Interfaces;

public interface ISchoolService
{
    Task<IEnumerable<SchoolListDto>> GetAllAsync(bool activeOnly, CancellationToken ct);
    Task<SchoolListDto> GetByIdAsync(Guid schoolId, CancellationToken ct);
    Task<SchoolListDto> CreateAsync(SchoolCreateDto dto, CancellationToken ct);
    Task<SchoolListDto> UpdateAsync(Guid schoolId, SchoolEditDto dto, CancellationToken ct);

    // Separate from UpdateAsync on purpose -- see the comment on SchoolListDto.
    Task SetActiveAsync(Guid schoolId, bool isActive, CancellationToken ct);
}
