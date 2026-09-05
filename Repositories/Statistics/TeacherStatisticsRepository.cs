using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Helpers;
using EduCore.Data;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Repositories.Interfaces;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class TeacherStatisticsRepository(
    AppDbContext context,
    IMarkRepository marks,
    IGradingCalculator grading) : StatisticsRepositoryBase(context), ITeacherStatisticsRepository
{

    private IQueryable<Models.ClassSubjectTeacher> AssignmentsInWindow(StatisticsQuery ctx)
    {
        var classIds = ClassesInScope(ctx).Select(c => c.Id);

        return Scope(_context.ClassSubjectTeachers, ctx)
            .Where(a => classIds.Contains(a.ClassId))
            .Where(a => a.EndedOn == null || a.EndedOn >= ctx.From);
    }

    private IQueryable<Models.Teacher> TeachersInScope(StatisticsQuery ctx)
    {
        var teachers = Scope(_context.Teachers, ctx);

        if (ctx.TeacherId is not null)
            return teachers.Where(t => t.Id == ctx.TeacherId);

        var assignedTeacherIds = AssignmentsInWindow(ctx).Select(a => a.TeacherId);

        return teachers.Where(t => assignedTeacherIds.Contains(t.Id));
    }

    public async Task<int> CountTeachersAsync(StatisticsQuery ctx, CancellationToken ct) =>
        await TeachersInScope(ctx).CountAsync(ct);

    public async Task<string?> GetTeacherNameAsync(
        StatisticsQuery ctx, Guid teacherId, CancellationToken ct) =>
        await (from t in Scope(_context.Teachers, ctx)
               where t.Id == teacherId
               join u in _context.Users on t.UserId equals u.Id
               select u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync(ct);

    // Projected straight into the DTO rather than into an anonymous type that is mapped after
    // materialising, so the full-list and paged readers below are the same query and cannot
    // drift apart. Ordered by school then name, tie-broken on the teacher id: two teachers can
    // share a name, and an unbroken tie lets LIMIT/OFFSET show one twice and the other never.
    private IQueryable<TeacherWorkloadDto> WorkloadQuery(StatisticsQuery ctx)
    {
        var assignments = AssignmentsInWindow(ctx);
        var lessons = LessonsInWindow(ctx);
        var activeEnrollments = EnrollmentsInWindow(ctx).Where(e => e.LeftAt == null);

        return from t in TeachersInScope(ctx)
               join u in _context.Users on t.UserId equals u.Id
               join sc in _context.Schools.IgnoreQueryFilters() on t.SchoolId equals sc.Id
               orderby sc.Name, u.LastName, u.FirstName, t.Id
               select new TeacherWorkloadDto(
                   t.Id,
                   u.FirstName + " " + u.LastName,
                   t.SchoolId,
                   sc.Name,
                   assignments
                       .Where(a => a.TeacherId == t.Id)
                       .Select(a => a.ClassId)
                       .Distinct()
                       .Count(),
                   assignments
                       .Where(a => a.TeacherId == t.Id)
                       .Select(a => a.SubjectId)
                       .Distinct()
                       .Count(),
                   activeEnrollments
                       .Where(e => assignments
                           .Where(a => a.TeacherId == t.Id)
                           .Select(a => a.ClassId)
                           .Contains(e.ClassId))
                       .Select(e => e.StudentId)
                       .Distinct()
                       .Count(),
                   lessons.Count(d => d.TeacherId == t.Id));
    }

    public async Task<IReadOnlyList<TeacherWorkloadDto>> GetWorkloadAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await WorkloadQuery(ctx).ToListAsync(ct);

    public Task<PagedResultDto<TeacherWorkloadDto>> GetWorkloadPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
        PageAsync(WorkloadQuery(ctx), pagination, ct);

    // Same shape as WorkloadQuery: projected into the DTO in the database so the paged reader
    // slices the identical query, with the same total ordering.
    private IQueryable<TeacherCoverageDto> CoverageQuery(StatisticsQuery ctx)
    {
        var lessons = LessonsInWindow(ctx);
        var attendances = Scope(_context.Attendances, ctx);
        var activeEnrollments = EnrollmentsInWindow(ctx).Where(e => e.LeftAt == null);
        var periodMarks = MarksInWindow(ctx);

        return from t in TeachersInScope(ctx)
               join u in _context.Users on t.UserId equals u.Id
               join sc in _context.Schools.IgnoreQueryFilters() on t.SchoolId equals sc.Id
               orderby sc.Name, u.LastName, u.FirstName, t.Id
               select new TeacherCoverageDto(
                   t.Id,
                   u.FirstName + " " + u.LastName,
                   t.SchoolId,
                   sc.Name,
                   lessons.Count(d => d.TeacherId == t.Id),
                   lessons
                       .Where(d => d.TeacherId == t.Id)
                       .Count(d => attendances.Count(a => a.DiaryEntryId == d.Id)
                                   < activeEnrollments.Count(e => e.ClassId == d.ClassId)),
                   periodMarks.Count(m => m.TeacherId == t.Id),
                   periodMarks.Count(m => m.TeacherId == t.Id
                                          && m.FirstMark != null
                                          && m.SecondMark != null),
                   periodMarks.Count(m => m.TeacherId == t.Id)
                   - periodMarks.Count(m => m.TeacherId == t.Id
                                            && m.FirstMark != null
                                            && m.SecondMark != null),
                   periodMarks.Count(m => m.TeacherId == t.Id) == 0
                       ? 0
                       : Math.Round(
                           periodMarks.Count(m => m.TeacherId == t.Id
                                                  && m.FirstMark != null
                                                  && m.SecondMark != null) * 100.0
                           / periodMarks.Count(m => m.TeacherId == t.Id), 2));
    }

    public async Task<IReadOnlyList<TeacherCoverageDto>> GetCoverageAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await CoverageQuery(ctx).ToListAsync(ct);

    public Task<PagedResultDto<TeacherCoverageDto>> GetCoveragePagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
        PageAsync(CoverageQuery(ctx), pagination, ct);

    public async Task<IReadOnlyList<(Guid ClassId, string ClassName, int Grade, Guid SubjectId,
        string SubjectName, int GradedMarks, double? Average, int Passed)>> GetPerformanceAsync(
        StatisticsQuery ctx, Guid teacherId, CancellationToken ct)
    {
        var pass = grading.PassValue;

        var first = grading.FirstMarkWeight;
        var second = grading.SecondMarkWeight;

        var rows = await (from m in MarksInWindow(ctx)
                          where m.TeacherId == teacherId && m.FirstMark != null && m.SecondMark != null
                          join c in ClassesInScope(ctx) on m.ClassId equals c.Id
                          join s in Scope(_context.Subjects, ctx) on m.SubjectId equals s.Id
                          group m.FirstMark!.Value * first + m.SecondMark!.Value * second
                              by new { ClassId = c.Id, ClassName = c.Name, c.Grade, SubjectId = s.Id, SubjectName = s.Name }
                          into g
                          orderby g.Key.ClassName, g.Key.SubjectName
                          select new
                          {
                              g.Key.ClassId,
                              g.Key.ClassName,
                              g.Key.Grade,
                              g.Key.SubjectId,
                              g.Key.SubjectName,
                              Count = g.Count(),
                              Average = (double?)g.Average(),
                              Passed = g.Count(value => value >= pass)
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => (r.ClassId, r.ClassName, r.Grade, r.SubjectId, r.SubjectName,
                r.Count, r.Average, r.Passed))
            .ToList();
    }
}
