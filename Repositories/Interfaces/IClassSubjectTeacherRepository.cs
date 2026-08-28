using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IClassSubjectTeacherRepository : IRepository<ClassSubjectTeacher>
{
    Task<IEnumerable<ClassSubjectTeacherListDto>> GetByClassAsync(
        Guid classId, bool includeEnded, CancellationToken ct);

    Task<IEnumerable<ClassSubjectTeacherListDto>> GetByTeacherAsync(
        Guid teacherId, bool includeEnded, CancellationToken ct);

    Task<ClassSubjectTeacher?> GetActiveAsync(Guid classId, Guid subjectId, CancellationToken ct);

    // Every active assignment on one class, tracked. The batch form of GetActiveAsync: the
    // elementary sync needs the answer for every subject at once, and asking subject by
    // subject cost one query each.
    Task<IReadOnlyList<ClassSubjectTeacher>> GetActiveForClassAsync(Guid classId, CancellationToken ct);

    Task<bool> TeachesClassAsync(Guid teacherId, Guid classId, CancellationToken ct);

    Task<ClassSubjectTeacherListDto?> GetDtoAsync(Guid assignmentId, CancellationToken ct);
}
