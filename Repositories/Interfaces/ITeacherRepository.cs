using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ITeacherRepository : IRepository<Teacher>
{
    Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Teacher?> GetWithSubjectsAsync(Guid teacherId, CancellationToken ct);
    Task<IEnumerable<Teacher>> GetAllWithUserAsync(CancellationToken ct);
    Task<IEnumerable<Teacher>> GetBySubjectAsync(Guid subjectId, CancellationToken ct);
}
