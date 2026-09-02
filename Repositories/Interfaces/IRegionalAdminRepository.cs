using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IRegionalAdminRepository : IRepository<RegionalAdmin>
{
    Task<IEnumerable<RegionalAdmin>> GetByStateAsync(Guid stateId, CancellationToken ct);
    Task<RegionalAdmin?> GetWithUserAsync(Guid regionalAdminId, CancellationToken ct);
    Task<RegionalAdmin?> GetTrackedAsync(Guid regionalAdminId, CancellationToken ct);
    Task<RegionalAdmin?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
