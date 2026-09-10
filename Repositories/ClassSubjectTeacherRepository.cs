using EduCore.Data;
using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class ClassSubjectTeacherRepository(AppDbContext context)
    : Repository<ClassSubjectTeacher>(context), IClassSubjectTeacherRepository
{
    private IQueryable<ClassSubjectTeacherListDto> Project(IQueryable<ClassSubjectTeacher> assignments) =>
        from cst in assignments
        join c in _context.Classes on cst.ClassId equals c.Id
        join s in _context.Subjects on cst.SubjectId equals s.Id
        join t in _context.Teachers on cst.TeacherId equals t.Id
        join u in _context.Users on t.UserId equals u.Id
        select new ClassSubjectTeacherListDto(
            cst.Id,
            cst.ClassId,
            c.Name,
            cst.SubjectId,
            s.Name,
            cst.TeacherId,
            u.Id,
            u.FirstName + " " + u.LastName,
            u.DeactivatedAt == null,
            cst.EndedOn);

    public async Task<ClassSubjectTeacherListDto?> GetDtoAsync(Guid assignmentId, CancellationToken ct) =>
        await Project(_context.ClassSubjectTeachers.AsNoTracking().Where(cst => cst.Id == assignmentId))
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<ClassSubjectTeacherListDto>> GetByClassAsync(
        Guid classId,
        bool includeEnded,
        CancellationToken ct)
    {
        var results = await Project(_context.ClassSubjectTeachers
                .AsNoTracking()
                .Where(cst => cst.ClassId == classId && (includeEnded || cst.EndedOn == null)))
            .ToListAsync(ct);

        return results.OrderBy(cst => cst.SubjectName);
    }

    public async Task<IEnumerable<ClassSubjectTeacherListDto>> GetByTeacherAsync(
        Guid teacherId,
        bool includeEnded,
        CancellationToken ct)
    {
        var results = await Project(_context.ClassSubjectTeachers
                .AsNoTracking()
                .Where(cst => cst.TeacherId == teacherId && (includeEnded || cst.EndedOn == null)))
            .ToListAsync(ct);

        return results.OrderBy(cst => cst.ClassName).ThenBy(cst => cst.SubjectName);
    }

    public async Task<ClassSubjectTeacher?> GetActiveAsync(Guid classId, Guid subjectId, CancellationToken ct) =>
        await _context.ClassSubjectTeachers
            .FirstOrDefaultAsync(
                cst => cst.ClassId == classId && cst.SubjectId == subjectId && cst.EndedOn == null,
                ct);

    public async Task<IReadOnlyList<ClassSubjectTeacher>> GetActiveForClassAsync(
        Guid classId, CancellationToken ct) =>
        await _context.ClassSubjectTeachers
            .Where(cst => cst.ClassId == classId && cst.EndedOn == null)
            .ToListAsync(ct);

    public async Task<bool> TeachesClassAsync(Guid teacherId, Guid classId, CancellationToken ct) =>
        await _context.ClassSubjectTeachers
            .AnyAsync(cst => cst.TeacherId == teacherId && cst.ClassId == classId && cst.EndedOn == null, ct);
}
