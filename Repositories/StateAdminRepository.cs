using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class StateAdminRepository(AppDbContext context)
    : Repository<StateAdmin>(context), IStateAdminRepository
{
    public async Task<IEnumerable<StateAdmin>> GetAllWithUserAsync(CancellationToken ct) =>
        await _context.StateAdmins
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.State)
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName)
            .ToListAsync(ct);

    public async Task<StateAdmin?> GetWithUserAsync(Guid stateAdminId, CancellationToken ct) =>
        await _context.StateAdmins
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.State)
            .FirstOrDefaultAsync(s => s.Id == stateAdminId, ct);

    public async Task<StateAdmin?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.StateAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);
}
