using EduCore.Data;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;
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
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .Include(a => a.User)
            .Include(a => a.School)
            .AsQueryable();

        if (schoolId is not null)
            query = query.Where(a => a.SchoolId == schoolId);

        return await query
            .OrderBy(a => a.User.LastName).ThenBy(a => a.User.FirstName)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Directorate> Items, int TotalCount)> GetPagedAsync(
        Guid? cityId, PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var query = _context.Directorates
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .Include(a => a.User)
            .Include(a => a.School)
            .AsQueryable();

        if (cityId is not null)
            query = query.Where(a => a.School.CityId == cityId);

        if (SearchTerm.Like(search) is { } term)
            query = query.Where(a =>
                EF.Functions.ILike(a.User.FirstName, term)
                || EF.Functions.ILike(a.User.LastName, term)
                || EF.Functions.ILike(a.User.Email, term)
                || EF.Functions.ILike(a.School.Name, term));

        return await query
            .OrderBy(a => a.User.LastName).ThenBy(a => a.User.FirstName).ThenBy(a => a.Id)
            .ToPageAsync(pagination, ct);
    }

    public async Task<IEnumerable<Directorate>> GetByCityAsync(Guid cityId, CancellationToken ct) =>
        await _context.Directorates
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .Include(a => a.User)
            .Include(a => a.School)
            .Where(a => a.School.CityId == cityId)
            .OrderBy(a => a.User.LastName).ThenBy(a => a.User.FirstName)
            .ToListAsync(ct);

    public async Task<Directorate?> GetWithUserAsync(Guid directorateId, CancellationToken ct) =>
        await _context.Directorates
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .Include(a => a.User)
            .Include(a => a.School)
            .FirstOrDefaultAsync(a => a.Id == directorateId, ct);

    public async Task<Directorate?> GetTrackedAsync(Guid directorateId, CancellationToken ct) =>
        await _context.Directorates
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(a => a.Id == directorateId, ct);

    public async Task<Directorate?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Directorates
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);
}
