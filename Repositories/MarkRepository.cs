using EduCore.Dtos.Statistics.Rows;
using EduCore.Helpers;
using EduCore.Data;
using EduCore.Dtos.Mark;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduCore.Repositories;

public class MarkRepository(
    AppDbContext context,
    IGradingCalculator grading) : Repository<Mark>(context), IMarkRepository
{

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
            m.FirstMark,
            m.SecondMark,
            null,
            m.WeightedValue,
            EF.Property<uint>(m, "Version"),
            g.ClosedAt != null);

    private IEnumerable<MarkListDto> WithFinalMarks(IEnumerable<MarkListDto> marks) =>
        marks.Select(m => m with { FinalMark = grading.FinalMark(m.WeightedValue) });

    public int? FinalMark(int? firstMark, int? secondMark) => grading.FinalMark(firstMark, secondMark);

    // Every reader uses the weighted value stored when the mark was written, under the weights
    // in force then. Recomputing from today's Grading weights would let a policy change for next
    // year rewrite last year's finals, pass rates and graduations.
    public IQueryable<ValuedMark> Value(IQueryable<Mark> marks) => ValueOf(marks);

    public IQueryable<double> WeightedValues(IQueryable<Mark> marks) => WeightedValuesOf(marks);

    public static IQueryable<double> WeightedValuesOf(IQueryable<Mark> marks) =>
        marks
            .Where(m => m.WeightedValue != null)
            .Select(m => m.WeightedValue!.Value);

    public static IQueryable<ValuedMark> ValueOf(IQueryable<Mark> marks) =>
        marks
            .Where(m => m.WeightedValue != null)
            .Select(m => new ValuedMark(
                m.Id,
                m.EnrollmentId,
                m.ClassId,
                m.SubjectId,
                m.TeacherId,
                m.GradingPeriodId,
                m.WeightedValue!.Value));

    private static IQueryable<Mark> Filter(IQueryable<Mark> marks, Guid? subjectId, Guid? gradingPeriodId)
    {
        if (subjectId is not null)
            marks = marks.Where(m => m.SubjectId == subjectId);

        if (gradingPeriodId is not null)
            marks = marks.Where(m => m.GradingPeriodId == gradingPeriodId);

        return marks;
    }

    public async Task<MarkListDto?> GetDtoAsync(Guid markId, CancellationToken ct)
    {
        var mark = await Project(_context.Marks.AsNoTracking().Where(m => m.Id == markId))
            .FirstOrDefaultAsync(ct);

        return mark is null ? null : WithFinalMarks([mark]).First();
    }

    // Makes the save conditional on the row still being at the version the client saw: EF puts
    // the original xmin in the UPDATE's WHERE, and a mismatch becomes a 409.
    public void ExpectVersion(Mark mark, uint version) =>
        _context.Entry(mark).Property<uint>("Version").OriginalValue = version;

    public void AddChange(MarkChange change) => _context.MarkChanges.Add(change);

    public async Task<IReadOnlyList<MarkChangeDto>> GetChangesAsync(Guid markId, CancellationToken ct) =>
        await (from c in _context.MarkChanges.AsNoTracking()
               join u in _context.Users on c.ChangedByUserId equals u.Id
               where c.MarkId == markId
               orderby c.ChangedAt descending
               select new MarkChangeDto(
                   c.ChangedAt, u.FirstName + " " + u.LastName,
                   c.OldFirstMark, c.OldSecondMark, c.NewFirstMark, c.NewSecondMark,
                   c.Reason, c.AfterClose))
            .ToListAsync(ct);

    public async Task<Mark?> GetForUpsertAsync(
        Guid enrollmentId, Guid subjectId, Guid gradingPeriodId, CancellationToken ct) =>
        await _context.Marks.FirstOrDefaultAsync(
            m => m.EnrollmentId == enrollmentId
                 && m.SubjectId == subjectId
                 && m.GradingPeriodId == gradingPeriodId,
            ct);

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

        var results = await Project(marks).ToListAsync(ct);

        return WithFinalMarks(results);
    }

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

        return WithFinalMarks(results).OrderBy(m => m.StudentName).ThenBy(m => m.SubjectName);
    }
}
