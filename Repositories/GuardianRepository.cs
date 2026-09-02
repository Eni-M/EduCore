using EduCore.Data;
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.UserId == userId, ct);

    public async Task<Guardian?> GetWithChildrenAsync(Guid guardianId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .Include(g => g.User)
            .Include(g => g.Children).ThenInclude(c => c.Student).ThenInclude(s => s.User)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

    public async Task<Guardian?> GetTrackedWithChildrenAsync(Guid guardianId, CancellationToken ct) =>
        await _context.Guardians
            .Include(g => g.Children)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

    public async Task<IEnumerable<Guardian>> GetBySchoolAsync(Guid schoolId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .Include(g => g.User)
            .Include(g => g.Children).ThenInclude(c => c.Student).ThenInclude(s => s.User)
            .IgnoreQueryFilters()
            .Where(g => g.Children.Any(c => c.SchoolId == schoolId))
            .OrderBy(g => g.User.LastName).ThenBy(g => g.User.FirstName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guardian>> SearchAsync(string query, int take, CancellationToken ct)
    {
        var term = query.Trim();

        return await _context.Guardians
            .AsNoTracking()
            .Include(g => g.User)
            .IgnoreQueryFilters()
            .Where(g =>
                EF.Functions.ILike(g.NationalId, $"%{term}%") ||
                EF.Functions.ILike(g.User.FirstName + " " + g.User.LastName, $"%{term}%"))
            .OrderBy(g => g.User.LastName).ThenBy(g => g.User.FirstName)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<Guardian?> GetGloballyAsync(Guid guardianId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .Include(g => g.User)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

    public async Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct) =>
        await _context.Guardians
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(g => g.NationalId == nationalId, ct);

    public async Task<GuardianStudent?> GetLinkAsync(Guid guardianId, Guid studentId, CancellationToken ct) =>
        await _context.GuardianStudents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(gs => gs.GuardianId == guardianId && gs.StudentId == studentId, ct);

    public async Task<IReadOnlyList<GuardianStudent>> GetContactsForStudentAsync(
        Guid studentId, CancellationToken ct) =>
        await _context.GuardianStudents
            .AsNoTracking()
            .Include(gs => gs.Guardian).ThenInclude(g => g.User)
            .IgnoreQueryFilters()
            .Where(gs => gs.StudentId == studentId)
            .OrderBy(gs => gs.Guardian.User.LastName).ThenBy(gs => gs.Guardian.User.FirstName)
            .ToListAsync(ct);

    public async Task<bool> LinkExistsAsync(Guid guardianId, Guid studentId, CancellationToken ct) =>
        await _context.GuardianStudents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(gs => gs.GuardianId == guardianId && gs.StudentId == studentId, ct);

    public async Task<bool> HasChildInClassAsync(Guid guardianId, Guid classId, CancellationToken ct) =>
        await _context.Enrollments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(
                e => e.ClassId == classId
                     && e.LeftAt == null
                     && _context.GuardianStudents
                         .IgnoreQueryFilters()
                         .Any(gs => gs.GuardianId == guardianId && gs.StudentId == e.StudentId),
                ct);

    public async Task AddLinkAsync(GuardianStudent link) =>
        await _context.GuardianStudents.AddAsync(link);

    public void RemoveLink(GuardianStudent link) =>
        _context.GuardianStudents.Remove(link);
}
