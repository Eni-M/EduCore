using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class RegionalAdminRepository(AppDbContext context)
    : Repository<RegionalAdmin>(context), IRegionalAdminRepository
{
    public async Task<IEnumerable<RegionalAdmin>> GetByStateAsync(Guid stateId, CancellationToken ct) =>
        await _context.RegionalAdmins
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.City).ThenInclude(c => c.State)
            .Where(r => r.City.StateId == stateId)
            .OrderBy(r => r.User.LastName).ThenBy(r => r.User.FirstName)
            .ToListAsync(ct);

    public async Task<RegionalAdmin?> GetWithUserAsync(Guid regionalAdminId, CancellationToken ct) =>
        await _context.RegionalAdmins
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.City).ThenInclude(c => c.State)
            .FirstOrDefaultAsync(r => r.Id == regionalAdminId, ct);

    public async Task<RegionalAdmin?> GetTrackedAsync(Guid regionalAdminId, CancellationToken ct) =>
        await _context.RegionalAdmins
            .FirstOrDefaultAsync(r => r.Id == regionalAdminId, ct);

    public async Task<RegionalAdmin?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.RegionalAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId, ct);
}
