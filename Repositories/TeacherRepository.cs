using EduCore.Data;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class TeacherRepository(AppDbContext context) : Repository<Teacher>(context), ITeacherRepository
{
    public async Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

    public async Task<bool> IsActiveAsync(Guid teacherId, CancellationToken ct) =>
        await _context.Teachers
            .AnyAsync(t => t.Id == teacherId && t.User.DeactivatedAt == null, ct);

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
            .Include(t => t.TeacherSubjects).ThenInclude(ts => ts.Subject)
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Teacher> Items, int TotalCount)> GetPagedAsync(
        Guid? subjectId, PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var query = _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.TeacherSubjects).ThenInclude(ts => ts.Subject)
            .AsQueryable();

        if (subjectId is not null)
            query = query.Where(t => t.TeacherSubjects.Any(ts => ts.SubjectId == subjectId));

        if (SearchTerm.Like(search) is { } term)
            query = query.Where(t =>
                EF.Functions.ILike(t.User.FirstName, term)
                || EF.Functions.ILike(t.User.LastName, term)
                || EF.Functions.ILike(t.User.Email, term)
                || t.TeacherSubjects.Any(ts => EF.Functions.ILike(ts.Subject.Name, term)));

        return await query
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName).ThenBy(t => t.Id)
            .ToPageAsync(pagination, ct);
    }

    public async Task<IEnumerable<Teacher>> GetBySubjectAsync(Guid subjectId, CancellationToken ct) =>
        await _context.Teachers
            .AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.TeacherSubjects.Any(ts => ts.SubjectId == subjectId))
            .OrderBy(t => t.User.LastName).ThenBy(t => t.User.FirstName)
            .ToListAsync(ct);
}
