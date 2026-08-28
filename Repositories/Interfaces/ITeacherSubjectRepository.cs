using EduCore.Dtos.TeacherSubject;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ITeacherSubjectRepository : IRepository<TeacherSubject>
{
    Task<IEnumerable<TeacherSubjectListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<TeacherSubject?> GetAsync(Guid teacherId, Guid subjectId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid teacherId, Guid subjectId, CancellationToken ct);

    // The batch forms of ExistsAsync, for the elementary sync: it asks the same question for
    // every subject of one teacher, or every teacher of one subject, and one query answers it.
    Task<IReadOnlyList<Guid>> GetSubjectIdsForTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetTeacherIdsForSubjectAsync(Guid subjectId, CancellationToken ct);

    Task<bool> IsInUseAsync(Guid teacherId, Guid subjectId, CancellationToken ct);
}
