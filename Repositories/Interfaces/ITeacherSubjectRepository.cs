using EduCore.Dtos.TeacherSubject;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ITeacherSubjectRepository : IRepository<TeacherSubject>
{
    Task<IEnumerable<TeacherSubjectListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<TeacherSubject?> GetAsync(Guid teacherId, Guid subjectId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid teacherId, Guid subjectId, CancellationToken ct);

    Task<bool> IsInUseAsync(Guid teacherId, Guid subjectId, CancellationToken ct);
}
