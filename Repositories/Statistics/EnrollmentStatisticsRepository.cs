using EduCore.Data;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class EnrollmentStatisticsRepository(AppDbContext context)
    : StatisticsRepositoryBase(context), IEnrollmentStatisticsRepository
{
    public async Task<int> CountActiveAsync(StatisticsContext ctx, CancellationToken ct) =>
        await EnrollmentsInWindow(ctx).CountAsync(e => e.LeftAt == null, ct);

    // Enrollments that began inside the window, whether or not they have since ended.
    public async Task<int> CountStartedAsync(StatisticsContext ctx, CancellationToken ct) =>
        await EnrollmentsInWindow(ctx)
            .CountAsync(e => e.EnrolledAt >= ctx.From && e.EnrolledAt <= ctx.To, ct);

    public async Task<int> CountExitedAsync(StatisticsContext ctx, CancellationToken ct) =>
        await EnrollmentsInWindow(ctx)
            .CountAsync(e => e.LeftAt != null && e.LeftAt >= ctx.From && e.LeftAt <= ctx.To, ct);

    public async Task<int> CountActiveOnAsync(StatisticsContext ctx, DateOnly on, CancellationToken ct)
    {
        var classIds = ClassesInScope(ctx).Select(c => c.Id);

        return await Scope(_context.Enrollments, ctx)
            .Where(e => classIds.Contains(e.ClassId))
            .CountAsync(e => e.EnrolledAt <= on && (e.LeftAt == null || e.LeftAt > on), ct);
    }

    public async Task<IReadOnlyList<CountRow>> GetExitsByReasonAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var rows = await EnrollmentsInWindow(ctx)
            .Where(e => e.LeftAt != null && e.LeftAt >= ctx.From && e.LeftAt <= ctx.To)
            .GroupBy(e => e.ExitReason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Enrollments closed before the reason was recorded come back as null, and are shown
        // as "Not recorded" rather than being dropped or lumped into a real reason.
        return rows
            .Select(r => new CountRow(
                r.Reason?.ToString() ?? "NotRecorded",
                r.Reason?.ToString() ?? "Not recorded",
                r.Count))
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public async Task<IReadOnlyList<CountRow>> GetByAcademicYearAsync(
        StatisticsContext ctx, CancellationToken ct)
    {
        var rows = await (from e in Scope(_context.Enrollments, ctx)
                          join c in Scope(_context.Classes, ctx) on e.ClassId equals c.Id
                          join y in Scope(_context.AcademicYears, ctx) on c.AcademicYearId equals y.Id
                          group e by new { y.Id, y.Name, y.StartDate }
                          into g
                          orderby g.Key.StartDate
                          select new
                          {
                              g.Key.Id,
                              g.Key.Name,
                              Count = g.Select(x => x.StudentId).Distinct().Count()
                          })
            .ToListAsync(ct);

        return rows.Select(r => new CountRow(r.Id.ToString(), r.Name, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<(Guid ClassId, string ClassName, int Grade, int? Capacity, int Enrolled)>>
        GetCapacityAsync(StatisticsContext ctx, CancellationToken ct)
    {
        var enrollments = EnrollmentsInWindow(ctx).Where(e => e.LeftAt == null);

        var rows = await (from c in ClassesInScope(ctx)
                          orderby c.Grade, c.Name
                          select new
                          {
                              c.Id,
                              c.Name,
                              c.Grade,
                              c.Capacity,
                              Enrolled = enrollments.Count(e => e.ClassId == c.Id)
                          })
            .ToListAsync(ct);

        return rows.Select(r => (r.Id, r.Name, r.Grade, r.Capacity, r.Enrolled)).ToList();
    }
}
