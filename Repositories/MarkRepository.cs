using EduCore.Data;
using EduCore.Dtos.Mark;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class MarkRepository(AppDbContext context) : Repository<Mark>(context), IMarkRepository
{
    // Sort on the source query or in memory -- EF cannot sort on a property of an
    // already-projected record.
    private IQueryable<MarkListDto> Project(IQueryable<Mark> marks) =>
        from m in marks
        join e in _context.Enrollments on m.EnrollmentId equals e.Id
        join st in _context.Students on e.StudentId equals st.Id
        join su in _context.Users on st.UserId equals su.Id
        join c in _context.Classes on m.ClassId equals c.Id
        join s in _context.Subjects on m.SubjectId equals s.Id
        join t in _context.Teachers on m.TeacherId equals t.Id
        join tu in _context.Users on t.UserId equals tu.Id
        join g in _context.GradingPeriods on m.GradingPeriodId equals g.Id
        select new MarkListDto(
            m.Id,
            m.EnrollmentId,
            su.FirstName + " " + su.LastName,
            c.Name,
            m.SubjectId,
            s.Name,
            m.TeacherId,
            tu.FirstName + " " + tu.LastName,
            m.GradingPeriodId,
            g.Name,
            m.Type,
            m.Value,
            m.Date);

    private static IQueryable<Mark> Filter(IQueryable<Mark> marks, Guid? subjectId, Guid? gradingPeriodId)
    {
        if (subjectId is not null)
            marks = marks.Where(m => m.SubjectId == subjectId);

        if (gradingPeriodId is not null)
            marks = marks.Where(m => m.GradingPeriodId == gradingPeriodId);

        return marks;
    }

    public async Task<MarkListDto?> GetDtoAsync(Guid markId, CancellationToken ct) =>
        await Project(_context.Marks.AsNoTracking().Where(m => m.Id == markId)).FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<MarkListDto>> GetByStudentAsync(
        Guid studentId,
        Guid? subjectId,
        Guid? gradingPeriodId,
        CancellationToken ct)
    {
        var enrollmentIds = _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => e.Id);

        var marks = Filter(
            _context.Marks.AsNoTracking().Where(m => enrollmentIds.Contains(m.EnrollmentId)),
            subjectId,
            gradingPeriodId);

        return await Project(marks.OrderByDescending(m => m.Date)).ToListAsync(ct);
    }

    // Sorted in memory: the student name lives on a joined user, and a class is small.
    public async Task<IEnumerable<MarkListDto>> GetByClassAsync(
        Guid classId,
        Guid? subjectId,
        Guid? gradingPeriodId,
        CancellationToken ct)
    {
        var marks = Filter(
            _context.Marks.AsNoTracking().Where(m => m.ClassId == classId),
            subjectId,
            gradingPeriodId);

        var results = await Project(marks).ToListAsync(ct);

        return results.OrderBy(m => m.StudentName).ThenByDescending(m => m.Date);
    }
}
