using EduCore.Dtos.Pagination;
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
        MarksInWindow(ctx).Where(m => m.WeightedValue != null);

    public async Task<AcademicTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct)
    {
        var values = Values(ctx);

        var graded = await values.CountAsync(ct);
        var incomplete = await MarksInWindow(ctx)
            .CountAsync(m => m.WeightedValue == null, ct);

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

    // Subject and class aggregates share their whole shape, so both the full-list and the
    // paged reader are built from one query each. Ordering by Name then Id: names are not
    // unique across schools, and an unbroken tie lets a row slip or repeat across a page edge.
    private IQueryable<AcademicGroup> BySubjectQuery(StatisticsQuery ctx)
    {
        var pass = grading.PassValue;

        return from m in GradedMarks(ctx)
               join s in Scope(_context.Subjects, ctx) on m.SubjectId equals s.Id
               group m.WeightedValue!.Value
                   by new { s.Id, s.Name }
               into g
               orderby g.Key.Name, g.Key.Id
               select new AcademicGroup(
                   g.Key.Id.ToString(),
                   g.Key.Name,
                   g.Count(),
                   g.Average(),
                   g.Count(value => value >= pass));
    }

    public async Task<IReadOnlyList<AcademicGroup>> GetBySubjectAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await BySubjectQuery(ctx).ToListAsync(ct);

    public Task<PagedResultDto<AcademicGroup>> GetBySubjectPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
        PageAsync(BySubjectQuery(ctx), pagination, ct);

    public Task<PagedResultDto<AcademicGroup>> GetByClassPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
        PageAsync(ByClassQuery(ctx), pagination, ct);

    private IQueryable<AcademicGroup> ByClassQuery(StatisticsQuery ctx)
    {
        var pass = grading.PassValue;

        return from m in GradedMarks(ctx)
               join c in ClassesInScope(ctx) on m.ClassId equals c.Id
               group m.WeightedValue!.Value
                   by new { c.Id, c.Name }
               into g
               orderby g.Key.Name, g.Key.Id
               select new AcademicGroup(
                   g.Key.Id.ToString(),
                   g.Key.Name,
                   g.Count(),
                   g.Average(),
                   g.Count(value => value >= pass));
    }

    public async Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await ByClassQuery(ctx).ToListAsync(ct);

    public async Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct)
    {
        var pass = grading.PassValue;

        return await (from m in GradedMarks(ctx).Where(m => classIds.Contains(m.ClassId))
                      join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                      group m.WeightedValue!.Value
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
        var pass = grading.PassValue;

        var rows = await (from m in GradedMarks(ctx)
                          join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                          group m.WeightedValue!.Value by c.Grade
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
        var pass = grading.PassValue;

        var rows = await (from m in GradedMarks(ctx)
                          join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                          join s in Scope(_context.Subjects, ctx) on m.SubjectId equals s.Id
                          group m.WeightedValue!.Value
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
        var (pass, atRiskUpper) = (grading.PassValue, grading.AtRiskUpperValue);

        var rows = await (from m in GradedMarks(ctx)
                          join e in Scope(_context.Enrollments, ctx) on m.EnrollmentId equals e.Id
                          join st in Scope(_context.Students, ctx) on e.StudentId equals st.Id
                          join u in _context.Users on st.UserId equals u.Id
                          join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                          group m.WeightedValue!.Value
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

    public async Task<PagedResultDto<AtRiskStudentDto>> GetAtRiskPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        // Same student-level rows as GetAtRiskAsync via the shared builder below, which
        // carries the ordering: by raw average, tie-broken by student id so LIMIT/OFFSET
        // pages are deterministic. Math.Round applies only when projecting DTOs, exactly
        // as GetAtRiskAsync does.
        var matches = AtRiskAverages(ctx);

        // The page before the count, deliberately. Both run either way, and this is the
        // query with the GROUP BY, the HAVING and the ORDER BY -- compiling it first means
        // a translation problem surfaces here rather than behind a COUNT that would have
        // translated fine on its own.
        var items = await matches
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalCount = await matches.CountAsync(ct);

        return new PagedResultDto<AtRiskStudentDto>(
            items.Select(r => new AtRiskStudentDto(
                r.StudentId,
                r.FirstName + " " + r.LastName,
                r.ClassId,
                r.ClassName,
                r.Grade,
                Math.Round(r.Average, 2),
                r.Failed,
                r.AtRisk)).ToList(),
            page, pageSize, totalCount);
    }

    private sealed record AtRiskAverage(
        Guid StudentId,
        string FirstName,
        string LastName,
        Guid ClassId,
        string ClassName,
        int Grade,
        double Average,
        int Failed,
        int AtRisk);

    private IQueryable<AtRiskAverage> AtRiskAverages(StatisticsQuery ctx)
    {
        var (pass, atRiskUpper) = (grading.PassValue, grading.AtRiskUpperValue);

        return from m in GradedMarks(ctx)
               join e in Scope(_context.Enrollments, ctx) on m.EnrollmentId equals e.Id
               join st in Scope(_context.Students, ctx) on e.StudentId equals st.Id
               join u in _context.Users on st.UserId equals u.Id
               join c in ClassesInScope(ctx) on m.ClassId equals c.Id
               group m.WeightedValue!.Value
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
               // Ordered over the group, not over the projected record. EF can put
               // AVG(...) in an ORDER BY beside its GROUP BY, but it cannot map a
               // property of the projected record back to the aggregate it came from --
               // ordering after the projection is what made this query untranslatable
               // and the endpoint return 500.
               orderby g.Average(), g.Key.Id
               select new AtRiskAverage(
                   g.Key.Id,
                   g.Key.FirstName,
                   g.Key.LastName,
                   g.Key.ClassId,
                   g.Key.ClassName,
                   g.Key.Grade,
                   g.Average(),
                   g.Count(value => value < pass),
                   g.Count(value => value >= pass && value < atRiskUpper));
    }

    public async Task<IReadOnlyList<AtRiskClassCount>> GetAtRiskCountsByClassAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct)
    {
        var atRiskUpper = grading.AtRiskUpperValue;

        // Same student-level risk rule as GetAtRiskAsync (average of weighted graded marks
        // below the at-risk threshold), but the database reduces to per-class counts: the
        // class summary only needs the number of at-risk students per class, never the
        // rows. The inner key keeps (student, class); names are functionally dependent on
        // those ids (one student has one name, one class has one name/grade), so the
        // grouping — and therefore every count — is identical to GetAtRiskAsync.
        // The Users join from GetAtRiskAsync is omitted: it only supplies names, and every
        // student has exactly one user (required UserId foreign key), so it can neither
        // filter nor duplicate rows here.
        var atRiskStudents = from m in GradedMarks(ctx).Where(m => classIds.Contains(m.ClassId))
                             join e in Scope(_context.Enrollments, ctx) on m.EnrollmentId equals e.Id
                             join st in Scope(_context.Students, ctx) on e.StudentId equals st.Id
                             join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                             group m.WeightedValue!.Value
                                 by new { st.Id, ClassId = c.Id }
                             into g
                             where g.Average() < atRiskUpper
                             select g.Key;

        return await (from s in atRiskStudents
                      group s by s.ClassId
                      into g
                      select new AtRiskClassCount(g.Key, g.Count()))
            .ToListAsync(ct);
    }
}
