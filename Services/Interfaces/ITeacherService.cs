using EduCore.Dtos.Teacher;
using EduCore.Dtos.TeacherSubject;

namespace EduCore.Services.Interfaces;

public interface ITeacherService
{
    Task<IEnumerable<TeacherListDto>> GetAllAsync(CancellationToken ct);
    Task<TeacherDetailDto> GetByIdAsync(Guid teacherId, CancellationToken ct);
    Task<IEnumerable<TeacherListDto>> GetBySubjectAsync(Guid subjectId, CancellationToken ct);

    Task<TeacherDetailDto> CreateAsync(TeacherCreateDto dto, CancellationToken ct);
    Task<TeacherDetailDto> UpdateAsync(Guid teacherId, TeacherEditDto dto, CancellationToken ct);

    Task<IEnumerable<TeacherSubjectListDto>> GetSubjectsAsync(Guid teacherId, CancellationToken ct);
    Task<TeacherSubjectListDto> AddSubjectAsync(TeacherSubjectCreateDto dto, CancellationToken ct);
    Task RemoveSubjectAsync(Guid teacherId, Guid subjectId, CancellationToken ct);
}
