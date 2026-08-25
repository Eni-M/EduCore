using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAdminRepository : IRepository<Admin>
{
    Task<IEnumerable<Admin>> GetAllAcrossSchoolsAsync(Guid? schoolId, CancellationToken ct);
    Task<Admin?> GetWithUserAsync(Guid adminId, CancellationToken ct);

    Task<Admin?> GetTrackedAsync(Guid adminId, CancellationToken ct);

    Task<Admin?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
