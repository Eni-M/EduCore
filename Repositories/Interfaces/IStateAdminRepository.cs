using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IStateAdminRepository : IRepository<StateAdmin>
{
    Task<IEnumerable<StateAdmin>> GetAllWithUserAsync(CancellationToken ct);
    Task<StateAdmin?> GetWithUserAsync(Guid stateAdminId, CancellationToken ct);
    Task<StateAdmin?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
