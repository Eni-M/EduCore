using EduCore.Dtos.ClassSubjectTeacher;

namespace EduCore.Services.Interfaces;

public interface IClassSubjectTeacherService
{
    Task<IEnumerable<ClassSubjectTeacherListDto>> GetByClassAsync(
        Guid classId, bool includeEnded, CancellationToken ct);

    Task<IEnumerable<ClassSubjectTeacherListDto>> GetByTeacherAsync(
        Guid teacherId, bool includeEnded, CancellationToken ct);

    Task<ClassSubjectTeacherListDto> CreateAsync(ClassSubjectTeacherCreateDto dto, CancellationToken ct);
    Task EndAsync(Guid assignmentId, ClassSubjectTeacherEndDto dto, CancellationToken ct);
}
