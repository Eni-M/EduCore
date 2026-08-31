using EduCore.Dtos.Statistics.Rows;
using EduCore.Data;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Helpers;
using EduCore.Repositories.Interfaces;
using EduCore.Repositories.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class AcademicStatisticsRepository(
    AppDbContext context,
    IMarkRepository marks,
    IGradingCalculator grading) : StatisticsRepositoryBase(context), IAcademicStatisticsRepository
{
    private IQueryable<double> Values(StatisticsQuery ctx) => marks.WeightedValues(MarksInWindow(ctx));

    private IQueryable<Models.Mark> GradedMarks(StatisticsQuery ctx) =>
        MarksInWindow(ctx).Where(m => m.FirstMark != null && m.SecondMark != null);

    public async Task<AcademicTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct)
    {
        var values = Values(ctx);

        var graded = await values.CountAsync(ct);
        var incomplete = await MarksInWindow(ctx)
            .CountAsync(m => m.FirstMark == null || m.SecondMark == null, ct);

        if (graded == 0)
            return new AcademicTotals(0, incomplete, null, 0, 0, 0);

        return new AcademicTotals(
            graded,
            incomplete,
            await values.AverageAsync(ct),
            await values.CountAsync(v => v >= grading.PassValue, ct),
            await values.CountAsync(v => v < grading.PassValue, ct),
            await values.CountAsync(
                v => v >= grading.AtRiskLowerValue && v < grading.AtRiskUpperValue, ct));
    }

    public async Task<IReadOnlyList<AcademicGroup>> GetBySubjectAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var (first, second, pass) = (grading.FirstMarkWeight, grading.SecondMarkWeight, grading.PassValue);

        return await (from m in GradedMarks(ctx)
                      join s in Scope(_context.Subjects, ctx) on m.SubjectId equals s.Id
                      group m.FirstMark!.Value * first + m.SecondMark!.Value * second
                          by new { s.Id, s.Name }
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
        StatisticsQuery ctx, CancellationToken ct)
    {
        var (first, second, pass) = (grading.FirstMarkWeight, grading.SecondMarkWeight, grading.PassValue);

        return await (from m in GradedMarks(ctx)
                      join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                      group m.FirstMark!.Value * first + m.SecondMark!.Value * second
                          by new { c.Id, c.Name }
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
        StatisticsQuery ctx, CancellationToken ct)
    {
        var (first, second, pass) = (grading.FirstMarkWeight, grading.SecondMarkWeight, grading.PassValue);

        var rows = await (from m in GradedMarks(ctx)
                          join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                          group m.FirstMark!.Value * first + m.SecondMark!.Value * second by c.Grade
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

    public async Task<IReadOnlyList<AcademicGroup>> GetSubjectGradeBaselinesAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var (first, second, pass) = (grading.FirstMarkWeight, grading.SecondMarkWeight, grading.PassValue);

        var rows = await (from m in GradedMarks(ctx)
                          join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                          join s in Scope(_context.Subjects, ctx) on m.SubjectId equals s.Id
                          group m.FirstMark!.Value * first + m.SecondMark!.Value * second
                              by new { m.SubjectId, s.Name, c.Grade }
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

    public static string BaselineKey(Guid subjectId, int grade) => subjectId + ":" + grade;

    public async Task<GradeDistributionResultDto> GetDistributionAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var values = await Values(ctx).ToListAsync(ct);
        var total = values.Count;

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
        StatisticsQuery ctx, CancellationToken ct)
    {
        var (first, second) = (grading.FirstMarkWeight, grading.SecondMarkWeight);
        var (pass, atRiskUpper) = (grading.PassValue, grading.AtRiskUpperValue);

        var rows = await (from m in GradedMarks(ctx)
                          join e in Scope(_context.Enrollments, ctx) on m.EnrollmentId equals e.Id
                          join st in Scope(_context.Students, ctx) on e.StudentId equals st.Id
                          join u in _context.Users on st.UserId equals u.Id
                          join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                          group m.FirstMark!.Value * first + m.SecondMark!.Value * second
                              by new
                              {
                                  st.Id,
                                  u.FirstName,
                                  u.LastName,
                                  ClassId = c.Id,
                                  ClassName = c.Name,
                                  c.Grade
                              }
                          into g
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
