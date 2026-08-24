using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    // Tracked, not AsNoTracking: the login path rehashes and saves the password when the
    // hasher asks for it, and the change-password path writes straight back to this row.
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) =>
        await _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken ct) =>
        await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == role)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);
}
