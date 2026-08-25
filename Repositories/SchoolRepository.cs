using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class SchoolRepository(AppDbContext context) : Repository<School>(context), ISchoolRepository
{
    public async Task<IEnumerable<School>> GetAllOrderedAsync(CancellationToken ct) =>
        await _context.Schools
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) =>
        await _context.Schools.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeSchoolId == null || s.Id != excludeSchoolId),
            ct);
}
