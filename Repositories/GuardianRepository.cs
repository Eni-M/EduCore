using EduCore.Data;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class GuardianRepository(AppDbContext context) : Repository<Guardian>(context), IGuardianRepository
{
    public async Task<Guardian?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .Include(g => g.Children)
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(g => g.UserId == userId, ct);

    public async Task<Guardian?> GetWithChildrenAsync(Guid guardianId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .Include(g => g.User)
            .Include(g => g.Children).ThenInclude(c => c.Student).ThenInclude(s => s.User)
            .Include(g => g.Children).ThenInclude(c => c.Student).ThenInclude(s => s.School)
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

    public async Task<Guardian?> GetTrackedWithChildrenAsync(Guid guardianId, CancellationToken ct) =>
        await _context.Guardians
            .Include(g => g.Children)
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

    public async Task<(IReadOnlyList<Guardian> Items, int TotalCount)> GetPagedBySchoolAsync(
        Guid schoolId, PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var query = _context.Guardians
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.User)
            .Include(g => g.Children).ThenInclude(c => c.Student).ThenInclude(s => s.User)
            .Include(g => g.Children).ThenInclude(c => c.Student).ThenInclude(s => s.School)
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .Where(g => g.Children.Any(c => c.SchoolId == schoolId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            var term = $"%{trimmed}%";
            // NationalId is encrypted at rest (exact match only); the rest support partial search.
            query = query.Where(g =>
                EF.Functions.ILike(g.User.FirstName, term)
                || EF.Functions.ILike(g.User.LastName, term)
                || EF.Functions.ILike(g.User.Email, term)
                || EF.Functions.ILike(g.PhoneNumber, term)
                || g.NationalId == trimmed);
        }

        query = query.OrderBy(g => g.User.LastName).ThenBy(g => g.User.FirstName).ThenBy(g => g.Id);
        return await query.ToPageAsync(pagination, ct);
    }

    public async Task<IReadOnlyList<Guardian>> SearchAsync(string query, int take, CancellationToken ct)
    {
        var term = query.Trim();

        return await _context.Guardians
            .AsNoTracking()
            .Include(g => g.User)
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            // NationalId is encrypted at rest, so only an exact match is possible (a substring
            // LIKE would run against ciphertext). The equality converter makes the exact match
            // work; names still support partial search.
            .Where(g =>
                g.NationalId == term ||
                EF.Functions.ILike(g.User.FirstName + " " + g.User.LastName, $"%{term}%"))
            .OrderBy(g => g.User.LastName).ThenBy(g => g.User.FirstName)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<Guardian?> GetGloballyAsync(Guid guardianId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .Include(g => g.User)
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

    public async Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .AnyAsync(g => g.NationalId == nationalId, ct);

    public async Task<GuardianStudent?> GetLinkAsync(Guid guardianId, Guid studentId, CancellationToken ct) =>
        await _context.GuardianStudents
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .FirstOrDefaultAsync(gs => gs.GuardianId == guardianId && gs.StudentId == studentId, ct);

    public async Task<IReadOnlyList<GuardianStudent>> GetContactsForStudentAsync(
        Guid studentId, CancellationToken ct) =>
        await _context.GuardianStudents
            .AsNoTracking()
            .Include(gs => gs.Guardian).ThenInclude(g => g.User)
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .Where(gs => gs.StudentId == studentId)
            .OrderBy(gs => gs.Guardian.User.LastName).ThenBy(gs => gs.Guardian.User.FirstName)
            .ToListAsync(ct);

    public async Task<bool> LinkExistsAsync(Guid guardianId, Guid studentId, CancellationToken ct) =>
        await _context.GuardianStudents
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .AnyAsync(gs => gs.GuardianId == guardianId && gs.StudentId == studentId, ct);

    public async Task<bool> HasChildInClassAsync(Guid guardianId, Guid classId, CancellationToken ct) =>
        await _context.Enrollments
            .AsNoTracking()
            .IgnoreQueryFilters(QueryFilters.SchoolOnly)
            .AnyAsync(
                e => e.ClassId == classId
                     && e.LeftAt == null
                     && _context.GuardianStudents
                         .IgnoreQueryFilters(QueryFilters.SchoolOnly)
                         .Any(gs => gs.GuardianId == guardianId && gs.StudentId == e.StudentId),
                ct);

    public async Task AddLinkAsync(GuardianStudent link) =>
        await _context.GuardianStudents.AddAsync(link);

    public void RemoveLink(GuardianStudent link) =>
        _context.GuardianStudents.Remove(link);
}
