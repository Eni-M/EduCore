using EduCore.Dtos.Curriculum;
using EduCore.Dtos.Subject;

namespace EduCore.Services.Interfaces;

public interface ICurriculumService
{
    Task<IEnumerable<GradeCurriculumEntryDto>> GetAllAsync(CancellationToken ct);
    Task<GradeCurriculumEntryDto> AddAsync(GradeCurriculumAddDto dto, CancellationToken ct);
    Task RemoveAsync(Guid id, CancellationToken ct);

    // The subjects a class of this grade may be assigned a teacher for. Backs the
    // assignment picker so the UI offers exactly what the API will accept.
    Task<IEnumerable<SubjectListDto>> GetSubjectsForGradeAsync(int grade, CancellationToken ct);
}
