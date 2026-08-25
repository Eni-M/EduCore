using EduCore.Dtos.Subject;

namespace EduCore.Services.Interfaces;

public interface ISubjectService
{
    Task<IEnumerable<SubjectListDto>> GetAllAsync(CancellationToken ct);
    Task<SubjectListDto> GetByIdAsync(Guid subjectId, CancellationToken ct);
    Task<SubjectListDto> CreateAsync(SubjectCreateDto dto, CancellationToken ct);
    Task<SubjectListDto> UpdateAsync(Guid subjectId, SubjectEditDto dto, CancellationToken ct);
    Task DeleteAsync(Guid subjectId, CancellationToken ct);
}
