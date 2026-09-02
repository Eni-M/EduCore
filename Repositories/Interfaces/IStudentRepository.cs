using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Student?> GetWithGuardiansAsync(Guid studentId, CancellationToken ct);

    Task<bool> PersonalNumberExistsAsync(string personalNumber, Guid? excludeStudentId, CancellationToken ct);

    Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool includeGraduated,
        CancellationToken ct);

    Task<IEnumerable<Student>> GetByClassAsync(Guid classId, CancellationToken ct);
}
