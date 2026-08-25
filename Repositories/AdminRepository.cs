using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class AdminRepository(AppDbContext context) : Repository<Admin>(context), IAdminRepository
{
    public async Task<IEnumerable<Admin>> GetAllAcrossSchoolsAsync(Guid? schoolId, CancellationToken ct)
    {
        var query = _context.Admins
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(a => a.User)
            .Include(a => a.School)
            .AsQueryable();

        if (schoolId is not null)
            query = query.Where(a => a.SchoolId == schoolId);

        return await query
            .OrderBy(a => a.User.LastName).ThenBy(a => a.User.FirstName)
            .ToListAsync(ct);
    }

    public async Task<Admin?> GetWithUserAsync(Guid adminId, CancellationToken ct) =>
        await _context.Admins
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(a => a.User)
            .Include(a => a.School)
            .FirstOrDefaultAsync(a => a.Id == adminId, ct);

    public async Task<Admin?> GetTrackedAsync(Guid adminId, CancellationToken ct) =>
        await _context.Admins
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == adminId, ct);

    public async Task<Admin?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Admins
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);
}
