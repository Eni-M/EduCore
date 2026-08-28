using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class SchoolRepository(AppDbContext context) : Repository<School>(context), ISchoolRepository
{
    public async Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct)
    {
        var query = _context.Schools.AsNoTracking().AsQueryable();

        if (activeOnly)
            query = query.Where(s => s.DeactivatedAt == null);

        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) =>
        await _context.Schools.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeSchoolId == null || s.Id != excludeSchoolId),
            ct);
}
