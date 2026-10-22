using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Data;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Enums;
using EduCore.Helpers;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class StudentStatisticsRepository(AppDbContext context)
    : StatisticsRepositoryBase(context), IStudentStatisticsRepository
{
    private IQueryable<Models.Student> StudentsInScope(StatisticsQuery ctx)
    {
        var students = Scope(_context.Students, ctx);

        if (ctx.CountryId is not null)
            students = students.Where(s => s.CountryId == ctx.CountryId);

        if (ctx.OriginCityId is not null)
            students = students.Where(s => s.OriginCityId == ctx.OriginCityId);

        if (ctx.ClassId is null && ctx.Grade is null && ctx.AcademicYearId is null)
            return students;

        var studentIds = EnrollmentsInWindow(ctx).Select(e => e.StudentId);

        return students.Where(s => studentIds.Contains(s.Id));
    }

    public async Task<StudentStatisticsDto> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct)
    {
        var students = StudentsInScope(ctx);

        var enrollments = Scope(_context.Enrollments, ctx);

        var newArrivals = await students
            .Where(s => enrollments
                .Where(e => e.StudentId == s.Id)
                .OrderBy(e => e.EnrolledAt)
                .Select(e => e.EnrolledAt)
                .FirstOrDefault() >= ctx.From)
            .CountAsync(s => enrollments
                .Where(e => e.StudentId == s.Id)
                .OrderBy(e => e.EnrolledAt)
                .Select(e => e.EnrolledAt)
                .FirstOrDefault() <= ctx.To, ct);

        // One round trip for the six plain counts: grouping on a constant turns them into
        // conditional aggregates in a single SELECT, instead of six queries to Neon in a row.
        var counts = await students
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(s => s.GraduationDate == null),
                Graduated = g.Count(s => s.GraduationDate != null),
                Female = g.Count(s => s.Gender == Gender.Female),
                Male = g.Count(s => s.Gender == Gender.Male),
                Unknown = g.Count(s => s.Gender == null),
            })
            .FirstOrDefaultAsync(ct);

        return new StudentStatisticsDto(
            counts?.Total ?? 0,
            counts?.Active ?? 0,
            counts?.Graduated ?? 0,
            newArrivals,
            counts?.Female ?? 0,
            counts?.Male ?? 0,
            counts?.Unknown ?? 0);
    }

    public async Task<IReadOnlyList<CountRow>> GetByGradeAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var rows = await (from e in EnrollmentsInWindow(ctx)
                          where e.LeftAt == null
                          join c in ClassesInScope(ctx) on e.ClassId equals c.Id
                          group e by c.Grade
                          into g
                          orderby g.Key
                          select new { Grade = g.Key, Count = g.Select(x => x.StudentId).Distinct().Count() })
            .ToListAsync(ct);

        return rows
            .Select(r => new CountRow(r.Grade.ToString(), "Grade " + r.Grade, r.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<ClassCountRow>> GetByClassAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await (from e in EnrollmentsInWindow(ctx)
               where e.LeftAt == null
               join c in ClassesInScope(ctx) on e.ClassId equals c.Id
               group e by new { c.Id, c.Name, c.Grade }
               into g
               orderby g.Key.Name
               select new ClassCountRow(
                   g.Key.Id,
                   g.Key.Name,
                   g.Key.Grade,
                   g.Select(x => x.StudentId).Distinct().Count()))
            .ToListAsync(ct);

    public async Task<PagedResultDto<ClassCountRow>> GetByClassPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        // Same row set as GetByClassAsync: active enrollments in the window joined to
        // in-scope classes. The total counts distinct classes; the page aggregates the
        // same groups with ORDER BY applied before Skip/Take, so concatenated pages
        // equal the full list without ever materialising it.
        var matches = from e in EnrollmentsInWindow(ctx)
                      where e.LeftAt == null
                      join c in ClassesInScope(ctx) on e.ClassId equals c.Id
                      select new { c.Id, c.Name, c.Grade, e.StudentId };

        var totalCount = await matches.Select(x => x.Id).Distinct().CountAsync(ct);

        var items = await (from x in matches
                           group x by new { x.Id, x.Name, x.Grade }
                           into g
                           // Name keeps the historical list order; Id makes it deterministic:
                           // duplicate class names across schools would otherwise let rows
                           // slip or repeat across LIMIT/OFFSET page boundaries.
                           orderby g.Key.Name, g.Key.Id
                           select new ClassCountRow(
                               g.Key.Id,
                               g.Key.Name,
                               g.Key.Grade,
                               g.Select(y => y.StudentId).Distinct().Count()))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<ClassCountRow>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<CountRow>> GetByAgeAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var reference = ctx.To;

        var birthDates = await StudentsInScope(ctx)
            .Where(s => s.GraduationDate == null)
            .Select(s => s.DateOfBirth)
            .ToListAsync(ct);

        return birthDates
            .Select(dob => AgeAt(dob, reference))
            .GroupBy(age => age)
            .OrderBy(g => g.Key)
            .Select(g => new CountRow(g.Key.ToString(), g.Key + " years", g.Count()))
            .ToList();
    }

    private static int AgeAt(DateOnly dateOfBirth, DateOnly on)
    {
        var age = on.Year - dateOfBirth.Year;

        if (on < dateOfBirth.AddYears(age))
            age--;

        return age;
    }

    public async Task<IReadOnlyList<(DateOnly Month, int Count)>> GetEnrollmentTrendAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var enrollments = await EnrollmentsInWindow(ctx)
            .Select(e => new { e.StudentId, e.EnrolledAt, e.LeftAt })
            .ToListAsync(ct);

        var months = new List<(DateOnly, int)>();

        for (var month = new DateOnly(ctx.From.Year, ctx.From.Month, 1);
             month <= ctx.To;
             month = month.AddMonths(1))
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            var pointInTime = monthEnd > ctx.To ? ctx.To : monthEnd;

            var count = enrollments
                .Where(e => e.EnrolledAt <= pointInTime && (e.LeftAt == null || e.LeftAt > pointInTime))
                .Select(e => e.StudentId)
                .Distinct()
                .Count();

            months.Add((month, count));
        }

        return months;
    }
}
