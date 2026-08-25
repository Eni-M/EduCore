using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class AcademicYearRepository(AppDbContext context)
    : Repository<AcademicYear>(context), IAcademicYearRepository
{
    public async Task<IEnumerable<AcademicYear>> GetAllOrderedAsync(CancellationToken ct) =>
        await _context.AcademicYears
            .AsNoTracking()
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct) =>
        await _context.AcademicYears.AnyAsync(a => a.Name.ToLower() == name.ToLower(), ct);

    public async Task<bool> OverlapsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct) =>
        await _context.AcademicYears.AnyAsync(a => a.StartDate <= endDate && startDate <= a.EndDate, ct);
}
