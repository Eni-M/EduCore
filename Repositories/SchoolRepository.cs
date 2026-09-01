using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class SchoolRepository(AppDbContext context) : Repository<School>(context), ISchoolRepository
{
    public async Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct)
    {
        var query = _context.Schools
            .AsNoTracking()
            .Include(s => s.City).ThenInclude(c => c!.State)
            .Include(s => s.Zone)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(s => s.DeactivatedAt == null);

        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct) =>
        await _context.Schools
            .Include(s => s.City).ThenInclude(c => c!.State)
            .Include(s => s.Zone)
            .FirstOrDefaultAsync(s => s.Id == schoolId, ct);

    public async Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) =>
        await _context.Schools.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeSchoolId == null || s.Id != excludeSchoolId),
            ct);
}
