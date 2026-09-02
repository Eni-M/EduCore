using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class DirectorateRepository(AppDbContext context) : Repository<Directorate>(context), IDirectorateRepository
{
    public async Task<IEnumerable<Directorate>> GetAllAcrossSchoolsAsync(Guid? schoolId, CancellationToken ct)
    {
        var query = _context.Directorates
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

    public async Task<IEnumerable<Directorate>> GetByCityAsync(Guid cityId, CancellationToken ct) =>
        await _context.Directorates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(a => a.User)
            .Include(a => a.School)
            .Where(a => a.School.CityId == cityId)
            .OrderBy(a => a.User.LastName).ThenBy(a => a.User.FirstName)
            .ToListAsync(ct);

    public async Task<Directorate?> GetWithUserAsync(Guid directorateId, CancellationToken ct) =>
        await _context.Directorates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(a => a.User)
            .Include(a => a.School)
            .FirstOrDefaultAsync(a => a.Id == directorateId, ct);

    public async Task<Directorate?> GetTrackedAsync(Guid directorateId, CancellationToken ct) =>
        await _context.Directorates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == directorateId, ct);

    public async Task<Directorate?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Directorates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);
}
