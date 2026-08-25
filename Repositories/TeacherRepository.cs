using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class TeacherRepository(AppDbContext context) : Repository<Teacher>(context), ITeacherRepository
{
    // Runs at login, before a school claim exists, so the filter would hide the very row
    // that says which school this person belongs to.
    public async Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

    public async Task<Teacher?> GetWithSubjectsAsync(Guid teacherId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.TeacherSubjects)
            .ThenInclude(ts => ts.Subject)
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);

    public async Task<IEnumerable<Teacher>> GetAllWithUserAsync(CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName)
            .ToListAsync(ct);

    public async Task<IEnumerable<Teacher>> GetBySubjectAsync(Guid subjectId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.TeacherSubjects.Any(ts => ts.SubjectId == subjectId))
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName)
            .ToListAsync(ct);
}
