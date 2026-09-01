using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
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
    private IQueryable<Models.Student> StudentsInScope(StatisticsQuery ctx)
    {
        var students = Scope(_context.Students, ctx);

        if (ctx.StateId is not null)
            students = students.Where(s => s.City != null && s.City.StateId == ctx.StateId);

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
