using EduCore.Dtos.Pagination;
using EduCore.Helpers;
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

    public async Task<PagedResultDto<School>> GetOrderedPagedAsync(
        bool activeOnly, PaginationQueryDto pagination, CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        var query = _context.Schools
            .AsNoTracking()
            .Include(s => s.City).ThenInclude(c => c!.State)
            .Include(s => s.Zone)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(s => s.DeactivatedAt == null);

        var totalCount = await query.CountAsync(ct);

        // Id after Name: school names are not unique nationwide, and without a tie-breaker
        // equal names can swap places between two LIMIT/OFFSET queries, so a row shows twice
        // on one page and never on the next.
        var items = await query
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<School>(items, page, pageSize, totalCount);
    }

    public async Task<IEnumerable<School>> GetByCityAsync(Guid cityId, bool activeOnly, CancellationToken ct)
    {
        var query = _context.Schools
            .AsNoTracking()
            .Include(s => s.City).ThenInclude(c => c!.State)
            .Include(s => s.Zone)
            .Where(s => s.CityId == cityId);

        if (activeOnly)
            query = query.Where(s => s.DeactivatedAt == null);

        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<IEnumerable<School>> GetByStateAsync(Guid stateId, bool activeOnly, CancellationToken ct)
    {
        var query = _context.Schools
            .AsNoTracking()
            .Include(s => s.City).ThenInclude(c => c!.State)
            .Include(s => s.Zone)
            .Where(s => s.City != null && s.City.StateId == stateId);

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
