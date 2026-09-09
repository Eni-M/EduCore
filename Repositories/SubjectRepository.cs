using EduCore.Data;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class SubjectRepository(AppDbContext context) : Repository<Subject>(context), ISubjectRepository
{
    public async Task<IEnumerable<Subject>> GetAllOrderedAsync(CancellationToken ct) =>
        await _context.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Subject> Items, int TotalCount)> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var query = _context.Subjects.AsNoTracking();

        if (SearchTerm.Like(search) is { } term)
            query = query.Where(s => EF.Functions.ILike(s.Name, term));

        // Id after Name as a tie-breaker: without one, equal names can swap between pages.
        return await query.OrderBy(s => s.Name).ThenBy(s => s.Id).ToPageAsync(pagination, ct);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct) =>
        await _context.Subjects.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeSubjectId == null || s.Id != excludeSubjectId),
            ct);

    public async Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct) =>
        await _context.TeacherSubjects.AnyAsync(ts => ts.SubjectId == subjectId, ct)
        || await _context.DiaryEntries.AnyAsync(d => d.SubjectId == subjectId, ct)
        || await _context.Marks.AnyAsync(m => m.SubjectId == subjectId, ct);
}
