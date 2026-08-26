using EduCore.Dtos.Statistics;
using EduCore.Data;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Enums;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class StudentStatisticsRepository(AppDbContext context)
    : StatisticsRepositoryBase(context), IStudentStatisticsRepository
{
    // Students reachable in this context: everyone in scope, narrowed to the classes the
    // filter selects when it selects any.
    private IQueryable<Models.Student> StudentsInScope(StatisticsContext ctx)
    {
        var students = Scope(_context.Students, ctx);

        if (ctx.ClassId is null && ctx.Grade is null && ctx.AcademicYearId is null)
            return students;

        var studentIds = EnrollmentsInWindow(ctx).Select(e => e.StudentId);

        return students.Where(s => studentIds.Contains(s.Id));
    }

    public async Task<StudentStatisticsDto> GetTotalsAsync(StatisticsContext ctx, CancellationToken ct)
    {
        var students = StudentsInScope(ctx);

        // A student counts as an arrival when their very first enrollment anywhere starts
        // inside the window. This is what §11 of the design calls a "new arrival" rather than
        // a transfer: Student is school-scoped, so someone moving in from another school is
        // simply a new record with no link to where they came from.
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

        return new StudentStatisticsDto(
            await students.CountAsync(ct),
            await students.CountAsync(s => s.GraduationDate == null, ct),
            await students.CountAsync(s => s.GraduationDate != null, ct),
            newArrivals,
            await students.CountAsync(s => s.Gender == Gender.Female, ct),
            await students.CountAsync(s => s.Gender == Gender.Male, ct),
            await students.CountAsync(s => s.Gender == null, ct));
    }

    public async Task<IReadOnlyList<CountRow>> GetByGradeAsync(
        StatisticsContext ctx, CancellationToken ct)
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
        StatisticsContext ctx, CancellationToken ct) =>
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

    public async Task<IReadOnlyList<CountRow>> GetByAgeAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        // Age is derived from the end of the window rather than from today, so a report run
        // for a past period gives the ages the students were at the time.
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
        StatisticsContext ctx, CancellationToken ct)
    {
        var enrollments = await EnrollmentsInWindow(ctx)
            .Select(e => new { e.StudentId, e.EnrolledAt, e.LeftAt })
            .ToListAsync(ct);

        var months = new List<(DateOnly, int)>();

        // Walks month ends across the window and counts who was enrolled on each. Done in
        // memory because the set is already narrowed to one window's enrollments, and a
        // month-by-month SQL equivalent would be far harder to read for no real gain.
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
