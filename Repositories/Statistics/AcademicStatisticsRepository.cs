using EduCore.Dtos.Statistics;
using EduCore.Grading;
using EduCore.Data;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Repositories.Interfaces;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

// Aggregates marks. It does not know how a final mark is calculated -- that rule belongs to
// MarkRepository.Value, because a final mark is real data about a student rather than a
// statistic. This class only groups and counts what that produces.
public class AcademicStatisticsRepository(
    AppDbContext context,
    IMarkRepository marks,
    IGradingCalculator grading) : StatisticsRepositoryBase(context), IAcademicStatisticsRepository
{

    private IQueryable<ValuedMark> ValuedMarks(StatisticsContext ctx) => marks.Value(MarksInWindow(ctx));

    public async Task<AcademicTotals> GetTotalsAsync(StatisticsContext ctx, CancellationToken ct)
    {
        var valued = ValuedMarks(ctx);

        var graded = await valued.CountAsync(ct);
        var incomplete = await MarksInWindow(ctx)
            .CountAsync(m => m.FirstMark == null || m.SecondMark == null, ct);

        if (graded == 0)
            return new AcademicTotals(0, incomplete, null, 0, 0, 0);

        return new AcademicTotals(
            graded,
            incomplete,
            await valued.AverageAsync(v => v.Value, ct),
            await valued.CountAsync(v => v.Value >= grading.PassValue, ct),
            await valued.CountAsync(v => v.Value < grading.PassValue, ct),
            await valued.CountAsync(
                v => v.Value >= grading.AtRiskLowerValue && v.Value < grading.AtRiskUpperValue, ct));
    }

    public async Task<IReadOnlyList<AcademicGroup>> GetBySubjectAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var pass = grading.PassValue;

        return await (from v in ValuedMarks(ctx)
                      join s in Scope(_context.Subjects, ctx) on v.SubjectId equals s.Id
                      group v.Value by new { s.Id, s.Name }
                      into g
                      orderby g.Key.Name
                      select new AcademicGroup(
                          g.Key.Id.ToString(),
                          g.Key.Name,
                          g.Count(),
                          g.Average(),
                          g.Count(value => value >= pass)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var pass = grading.PassValue;

        return await (from v in ValuedMarks(ctx)
                      join c in ClassesInScope(ctx) on v.ClassId equals c.Id
                      group v.Value by new { c.Id, c.Name }
                      into g
                      orderby g.Key.Name
                      select new AcademicGroup(
                          g.Key.Id.ToString(),
                          g.Key.Name,
                          g.Count(),
                          g.Average(),
                          g.Count(value => value >= pass)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AcademicGroup>> GetByGradeAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var pass = grading.PassValue;

        var rows = await (from v in ValuedMarks(ctx)
                          join c in ClassesInScope(ctx) on v.ClassId equals c.Id
                          group v.Value by c.Grade
                          into g
                          orderby g.Key
                          select new
                          {
                              Grade = g.Key,
                              Count = g.Count(),
                              Average = (double?)g.Average(),
                              Passed = g.Count(value => value >= pass)
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => new AcademicGroup(
                r.Grade.ToString(), "Grade " + r.Grade, r.Count, r.Average, r.Passed))
            .ToList();
    }

    // The school-wide average for a subject at a grade level. This is the baseline every
    // per-class and per-teacher figure is reported against, so a number is never shown bare.
    public async Task<IReadOnlyList<AcademicGroup>> GetSubjectGradeBaselinesAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var pass = grading.PassValue;

        var rows = await (from v in ValuedMarks(ctx)
                          join c in ClassesInScope(ctx) on v.ClassId equals c.Id
                          join s in Scope(_context.Subjects, ctx) on v.SubjectId equals s.Id
                          group v.Value by new { v.SubjectId, s.Name, c.Grade }
                          into g
                          select new
                          {
                              g.Key.SubjectId,
                              g.Key.Name,
                              g.Key.Grade,
                              Count = g.Count(),
                              Average = (double?)g.Average(),
                              Passed = g.Count(value => value >= pass)
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => new AcademicGroup(
                BaselineKey(r.SubjectId, r.Grade),
                r.Name + ", grade " + r.Grade,
                r.Count,
                r.Average,
                r.Passed))
            .ToList();
    }

    // Pairs subject with grade level so a caller can look up an exact baseline without
    // scanning the list.
    public static string BaselineKey(Guid subjectId, int grade) => subjectId + ":" + grade;

    // Returns the raw buckets. Whether they are safe to show is the service's call, since it
    // is the one holding the suppressor.
    public async Task<GradeDistributionResultDto> GetDistributionAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var values = await ValuedMarks(ctx).Select(v => v.Value).ToListAsync(ct);
        var total = values.Count;

        // Rounded in memory over an already-reduced list, so it rounds identically to
        // GradingCalculator rather than depending on how the database rounds.
        var counts = values
            .GroupBy(value => (int)Math.Round(value, MidpointRounding.AwayFromZero))
            .ToDictionary(g => g.Key, g => g.Count());

        var buckets = Enumerable.Range(1, 5)
            .Select(mark =>
            {
                var count = counts.GetValueOrDefault(mark);
                return new GradeDistributionDto(
                    mark, count, total == 0 ? 0 : Math.Round(count * 100.0 / total, 2));
            })
            .ToList();

        return new GradeDistributionResultDto(total, false, buckets);
    }

    public async Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var pass = grading.PassValue;
        var atRiskUpper = grading.AtRiskUpperValue;

        // Grouped per student across their subjects, so "at risk" reflects the whole record
        // rather than a single weak subject.
        var rows = await (from v in ValuedMarks(ctx)
                          join e in Scope(_context.Enrollments, ctx) on v.EnrollmentId equals e.Id
                          join st in Scope(_context.Students, ctx) on e.StudentId equals st.Id
                          join u in _context.Users on st.UserId equals u.Id
                          join c in ClassesInScope(ctx) on v.ClassId equals c.Id
                          group v.Value by new
                          {
                              st.Id,
                              u.FirstName,
                              u.LastName,
                              ClassId = c.Id,
                              ClassName = c.Name,
                              c.Grade
                          }
                          into g
                          // Same rule as IGradingCalculator.IsStudentAtRisk, written as the
                          // threshold comparison it is defined by -- a method call here would
                          // not translate to SQL. Both read AtRiskUpperValue, and a test pins
                          // them together.
                          where g.Average() < atRiskUpper
                          select new
                          {
                              g.Key.Id,
                              g.Key.FirstName,
                              g.Key.LastName,
                              g.Key.ClassId,
                              g.Key.ClassName,
                              g.Key.Grade,
                              Average = g.Average(),
                              Failed = g.Count(value => value < pass),
                              AtRisk = g.Count(value => value >= pass && value < atRiskUpper)
                          })
            .ToListAsync(ct);

        return rows
            .OrderBy(r => r.Average)
            .Select(r => new AtRiskStudentDto(
                r.Id,
                r.FirstName + " " + r.LastName,
                r.ClassId,
                r.ClassName,
                r.Grade,
                Math.Round(r.Average, 2),
                r.Failed,
                r.AtRisk))
            .ToList();
    }
}
