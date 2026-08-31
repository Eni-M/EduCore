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
    IGradingCalculator grading,
    IOptions<GradingOptions> weights) : Repository<Mark>(context), IMarkRepository
{
    private readonly GradingOptions _weights = weights.Value;

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
            null);

    private IEnumerable<MarkListDto> WithFinalMarks(IEnumerable<MarkListDto> marks) =>
        marks.Select(m => m with { FinalMark = grading.FinalMark(m.FirstMark, m.SecondMark) });

    public int? FinalMark(int? firstMark, int? secondMark) => grading.FinalMark(firstMark, secondMark);

    public IQueryable<ValuedMark> Value(IQueryable<Mark> marks) => Value(marks, _weights);

    public IQueryable<double> WeightedValues(IQueryable<Mark> marks) =>
        WeightedValues(marks, _weights);

    public static IQueryable<double> WeightedValues(IQueryable<Mark> marks, GradingOptions weights) =>
        marks
            .Where(m => m.FirstMark != null && m.SecondMark != null)
            .Select(m => m.FirstMark!.Value * weights.FirstMarkWeight
                         + m.SecondMark!.Value * weights.SecondMarkWeight);

    public static IQueryable<ValuedMark> Value(IQueryable<Mark> marks, GradingOptions weights) =>
        marks
            .Where(m => m.FirstMark != null && m.SecondMark != null)
            .Select(m => new ValuedMark(
                m.Id,
                m.EnrollmentId,
                m.ClassId,
                m.SubjectId,
                m.TeacherId,
                m.GradingPeriodId,
                m.FirstMark!.Value * weights.FirstMarkWeight
                + m.SecondMark!.Value * weights.SecondMarkWeight));

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
