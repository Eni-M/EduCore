using EduCore.Dtos.Curriculum;

namespace EduCore.Services.Interfaces;

public interface ICurriculumService
{
    Task<IEnumerable<GradeCurriculumEntryDto>> GetAllAsync(CancellationToken ct);
    Task<GradeCurriculumEntryDto> AddAsync(GradeCurriculumAddDto dto, CancellationToken ct);
    Task RemoveAsync(Guid id, CancellationToken ct);
}
