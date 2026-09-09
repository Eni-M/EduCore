using EduCore.Dtos.Pagination;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISchoolRepository : IRepository<School>
{
    Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct);

    // The same ordering, paged in the database. Backs the platform comparison table, which is
    // one row per school nationwide.
    Task<PagedResultDto<School>> GetOrderedPagedAsync(
        bool activeOnly, PaginationQueryDto pagination, CancellationToken ct);
    // Filtered and paged in the database. cityId/stateId narrow to the caller's tier, so a
    // regional or state admin searches their own scope rather than the whole country.
    Task<(IReadOnlyList<School> Items, int TotalCount)> GetPagedAsync(
        bool activeOnly, Guid? cityId, Guid? stateId,
        PaginationQueryDto pagination, string? search, CancellationToken ct);

    Task<IEnumerable<School>> GetByCityAsync(Guid cityId, bool activeOnly, CancellationToken ct);
    Task<IEnumerable<School>> GetByStateAsync(Guid stateId, bool activeOnly, CancellationToken ct);
    Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct);
}
