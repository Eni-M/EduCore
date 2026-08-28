using EduCore.Data;
using EduCore.Dtos.TeacherSubject;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class TeacherSubjectRepository(AppDbContext context)
    : Repository<TeacherSubject>(context), ITeacherSubjectRepository
{
    public async Task<IEnumerable<TeacherSubjectListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
        await _context.TeacherSubjects
            .AsNoTracking()
            .Where(ts => ts.TeacherId == teacherId)
            .OrderBy(ts => ts.Subject.Name)
            .Select(ts => new TeacherSubjectListDto(ts.Id, ts.SubjectId, ts.Subject.Name))
            .ToListAsync(ct);

    public async Task<TeacherSubject?> GetAsync(Guid teacherId, Guid subjectId, CancellationToken ct) =>
        await _context.TeacherSubjects
            .FirstOrDefaultAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId, ct);

    public async Task<bool> ExistsAsync(Guid teacherId, Guid subjectId, CancellationToken ct) =>
        await _context.TeacherSubjects
            .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId, ct);

    public async Task<IReadOnlyList<Guid>> GetSubjectIdsForTeacherAsync(
        Guid teacherId, CancellationToken ct) =>
        await _context.TeacherSubjects
            .AsNoTracking()
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => ts.SubjectId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetTeacherIdsForSubjectAsync(
        Guid subjectId, CancellationToken ct) =>
        await _context.TeacherSubjects
            .AsNoTracking()
            .Where(ts => ts.SubjectId == subjectId)
            .Select(ts => ts.TeacherId)
            .ToListAsync(ct);

    public async Task<bool> IsInUseAsync(Guid teacherId, Guid subjectId, CancellationToken ct) =>
        await _context.ClassSubjectTeachers
            .AnyAsync(cst => cst.TeacherId == teacherId && cst.SubjectId == subjectId, ct)
        || await _context.DiaryEntries
            .AnyAsync(d => d.TeacherId == teacherId && d.SubjectId == subjectId, ct);
}
