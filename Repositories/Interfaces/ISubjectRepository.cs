using EduCore.Dtos.Pagination;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISubjectRepository : IRepository<Subject>
{
    Task<IEnumerable<Subject>> GetAllOrderedAsync(CancellationToken ct);

    // Filtered and paged in the database, so a search reads the matching rows rather than
    // the whole catalogue.
    Task<(IReadOnlyList<Subject> Items, int TotalCount)> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct);

    Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct);
}
