using EduCore.Dtos.Pagination;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IRegionalAdminRepository : IRepository<RegionalAdmin>
{
    // Filtered and paged in the database, within one state.
    Task<(IReadOnlyList<RegionalAdmin> Items, int TotalCount)> GetPagedAsync(
        Guid stateId, PaginationQueryDto pagination, string? search, CancellationToken ct);

    Task<IEnumerable<RegionalAdmin>> GetByStateAsync(Guid stateId, CancellationToken ct);
    Task<RegionalAdmin?> GetWithUserAsync(Guid regionalAdminId, CancellationToken ct);
    Task<RegionalAdmin?> GetTrackedAsync(Guid regionalAdminId, CancellationToken ct);
    Task<RegionalAdmin?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
