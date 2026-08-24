using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class StudentRepository(AppDbContext context) : Repository<Student>(context), IStudentRepository
{
    // Used during login, before any school_id claim exists, so the school filter would otherwise hide the very row that tells us which school the caller belongs to.
    public async Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<Student?> GetWithUserAsync(Guid studentId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

    public async Task<Student?> GetWithSchoolAsync(Guid studentId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.School)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

    public async Task<Student?> GetWithParentsAsync(Guid studentId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Parents)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

    public async Task<Student?> GetProfileAsync(Guid studentId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.School)
            .Include(s => s.Parents)
            .Include(s => s.Enrollments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

    public async Task<Student?> GetByPersonalNumberAsync(string personalNumber, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.PersonalNumber == personalNumber, ct);

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

    public async Task<IEnumerable<Student>> GetAllWithUserAsync(bool includeGraduated, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => includeGraduated || s.GraduationDate == null)
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName)
            .ToListAsync(ct);

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

        // Counted before paging so the caller can report totals against the whole filtered set rather than the slice it just received.
  
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Student>> GetByIdsAsync(IEnumerable<Guid> studentIds, CancellationToken ct)
    {
        var ids = studentIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Student>> GetByClassAsync(Guid classId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.Enrollments.Any(e => e.ClassId == classId && e.LeftAt == null))
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName)
            .ToListAsync(ct);

    // Students who still need placing for a given year: no active enrollment in any class belonging to that academic year, and not already graduated.
    public async Task<IEnumerable<Student>> GetUnassignedAsync(Guid academicYearId, CancellationToken ct)
    {
        var classIdsForYear = _context.Classes
            .Where(c => c.AcademicYearId == academicYearId)
            .Select(c => c.Id);

        return await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.GraduationDate == null
                        && !s.Enrollments.Any(e => e.LeftAt == null && classIdsForYear.Contains(e.ClassId)))
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName)
            .ToListAsync(ct);
    }

    public async Task<bool> IsEnrolledInClassAsync(Guid studentId, Guid classId, CancellationToken ct) =>
        await _context.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.ClassId == classId && e.LeftAt == null, ct);

    // A student sits in one class at a time, so at most one enrollment is ever open.
    public async Task<Enrollment?> GetActiveEnrollmentAsync(Guid studentId, CancellationToken ct) =>
        await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && e.LeftAt == null)
            .OrderByDescending(e => e.EnrolledAt)
            .FirstOrDefaultAsync(ct);

    public async Task<int> CountActiveInClassAsync(Guid classId, CancellationToken ct) =>
        await _context.Enrollments
            .CountAsync(e => e.ClassId == classId && e.LeftAt == null, ct);

    public async Task<IEnumerable<Student>> GetByParentPhoneAsync(string phoneNumber, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Parents)
            .Where(s => s.Parents.Any(p => p.PhoneNumber == phoneNumber))
            .ToListAsync(ct);
}
