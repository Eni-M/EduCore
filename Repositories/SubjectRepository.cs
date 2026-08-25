using EduCore.Data;
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

    public async Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct) =>
        await _context.Subjects.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeSubjectId == null || s.Id != excludeSubjectId),
            ct);

    public async Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct) =>
        await _context.TeacherSubjects.AnyAsync(ts => ts.SubjectId == subjectId, ct)
        || await _context.DiaryEntries.AnyAsync(d => d.SubjectId == subjectId, ct)
        || await _context.Marks.AnyAsync(m => m.SubjectId == subjectId, ct);
}
