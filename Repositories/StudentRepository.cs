using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class StudentRepository(AppDbContext context) : Repository<Student>(context), IStudentRepository
{
    public async Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<Student?> GetWithGuardiansAsync(Guid studentId, CancellationToken ct) =>
        await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Country)
            .Include(s => s.OriginCity)
            .Include(s => s.Guardians).ThenInclude(gs => gs.Guardian).ThenInclude(g => g.User)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

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
            var trimmed = search.Trim();
            var term = $"%{trimmed}%";
            // PersonalNumber is deterministically encrypted at rest, so only exact
            // equality translates (substring ILike can't match ciphertext) — a full
            // national ID matches, a partial one does not.
            query = query.Where(s =>
                EF.Functions.ILike(s.User.FirstName, term)
                || EF.Functions.ILike(s.User.LastName, term)
                || EF.Functions.ILike(s.User.Email, term)
                || s.PersonalNumber == trimmed);
        }

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
