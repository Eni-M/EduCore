using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class StudentRepository(AppDbContext context) : Repository<Student>(context), IStudentRepository
{
    // Runs at login, before a school claim exists, so the filter would hide the very row
    // that says which school this person belongs to.
    public async Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<Student?> GetWithParentsAsync(Guid studentId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.City).ThenInclude(c => c!.State)
            .Include(s => s.Parents)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

    // A national ID is unique across all schools, so this one check looks past the filter.
    public async Task<bool> PersonalNumberExistsAsync(
        string personalNumber,
        Guid? excludeStudentId,
        CancellationToken ct) =>
        await _context.Students
            .IgnoreQueryFilters()
            .AnyAsync(
                s => s.PersonalNumber == personalNumber
                     && (excludeStudentId == null || s.Id != excludeStudentId),
                ct);

    public async Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool includeGraduated,
        CancellationToken ct)
    {
        var query = _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => includeGraduated || s.GraduationDate == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.User.FirstName, term)
                || EF.Functions.ILike(s.User.LastName, term)
                || EF.Functions.ILike(s.User.Email, term));
        }

        // Counted before paging, so the total covers the whole filtered set.
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName).ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Student>> GetByClassAsync(Guid classId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.Enrollments.Any(e => e.ClassId == classId && e.LeftAt == null))
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName)
            .ToListAsync(ct);
}
