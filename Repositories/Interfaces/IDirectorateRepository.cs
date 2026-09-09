using EduCore.Dtos.Pagination;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IDirectorateRepository : IRepository<Directorate>
{
    // Filtered and paged in the database, narrowed to one city for a regional admin.
    Task<(IReadOnlyList<Directorate> Items, int TotalCount)> GetPagedAsync(
        Guid? cityId, PaginationQueryDto pagination, string? search, CancellationToken ct);

    Task<IEnumerable<Directorate>> GetAllAcrossSchoolsAsync(Guid? schoolId, CancellationToken ct);
    Task<IEnumerable<Directorate>> GetByCityAsync(Guid cityId, CancellationToken ct);
    Task<Directorate?> GetWithUserAsync(Guid directorateId, CancellationToken ct);

    Task<Directorate?> GetTrackedAsync(Guid directorateId, CancellationToken ct);

    Task<Directorate?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
