using EduCore.Dtos.Pagination;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ITeacherRepository : IRepository<Teacher>
{
    // Filtered and paged in the database. subjectId narrows to teachers qualified in it;
    // the search covers name, email and the subject names themselves.
    Task<(IReadOnlyList<Teacher> Items, int TotalCount)> GetPagedAsync(
        Guid? subjectId, PaginationQueryDto pagination, string? search, CancellationToken ct);

    Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Teacher?> GetWithSubjectsAsync(Guid teacherId, CancellationToken ct);
    Task<IEnumerable<Teacher>> GetAllWithUserAsync(CancellationToken ct);
    Task<IEnumerable<Teacher>> GetBySubjectAsync(Guid subjectId, CancellationToken ct);
}
