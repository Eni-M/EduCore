using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ITeacherRepository : IRepository<Teacher>
{
    Task<Teacher?> GetWithSchoolAsync(Guid teacherId, CancellationToken ct);
    Task<Teacher?> GetWithSubjectsAsync(Guid teacherId, CancellationToken ct);
    Task<IEnumerable<Teacher>> GetBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task<(Teacher? Teacher, List<Guid> ClassIds)> GetWithClassesAsync(Guid teacherId, CancellationToken ct);
    Task<IEnumerable<Teacher>> GetAllWithSchoolAsync(CancellationToken ct);

    // Cross-school lookup for system/SuperAdmin tooling -- see the implementation.
    Task<IEnumerable<Teacher>> GetBySchoolAsync(Guid schoolId, CancellationToken ct);

    Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
