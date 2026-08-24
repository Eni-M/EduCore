using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class TeacherRepository(AppDbContext context) : Repository<Teacher>(context), ITeacherRepository
{
  
    public async Task<Teacher?> GetWithSchoolAsync(Guid teacherId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.School)
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);

    public async Task<Teacher?> GetWithSubjectsAsync(Guid teacherId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.TeacherSubjects)
            .ThenInclude(ts => ts.Subject)
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);

    public async Task<IEnumerable<Teacher>> GetBySubjectAsync(Guid subjectId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.TeacherSubjects.Any(ts => ts.SubjectId == subjectId))
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName)
            .ToListAsync(ct);

    public async Task<(Teacher? Teacher, List<Guid> ClassIds)> GetWithClassesAsync(Guid teacherId, CancellationToken ct)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);

        if (teacher is null)
            return (null, []);

        var classIds = await _context.ClassSubjectTeachers
            .Where(cst => cst.TeacherId == teacherId && cst.EndedOn == null)
            .Select(cst => cst.ClassId)
            .Distinct()
            .ToListAsync(ct);

        return (teacher, classIds);
    }

    public async Task<IEnumerable<Teacher>> GetAllWithSchoolAsync(CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.School)
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName)
            .ToListAsync(ct);

    // The global query filter already pins every Teacher query to the caller's own school,
    // so filtering by some *other* schoolId on top of it can only ever return nothing. A
    // by-school lookup is only meaningful as a deliberate cross-school one, which is what
    // IgnoreQueryFilters() plus an explicit SchoolId predicate expresses. Callers must gate
    // this on a SuperAdmin check -- it is the read-side twin of SchoolIsolationScope.Suppress().
    public async Task<IEnumerable<Teacher>> GetBySchoolAsync(Guid schoolId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .Include(t => t.School)
            .Where(t => t.SchoolId == schoolId)
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName)
            .ToListAsync(ct);

    // Used during login, before any school_id claim exists, so the school filter would
    // otherwise hide the very row that tells us which school the caller belongs to.
    public async Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
}
