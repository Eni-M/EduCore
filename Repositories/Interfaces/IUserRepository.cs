using EduCore.Enums;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken ct);
}
