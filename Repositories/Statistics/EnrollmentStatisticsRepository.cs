using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Data;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class EnrollmentStatisticsRepository(AppDbContext context)
    : StatisticsRepositoryBase(context), IEnrollmentStatisticsRepository
{
    public async Task<int> CountActiveAsync(StatisticsQuery ctx, CancellationToken ct) =>
        await EnrollmentsInWindow(ctx).CountAsync(e => e.LeftAt == null, ct);

    public async Task<int> CountStartedAsync(StatisticsQuery ctx, CancellationToken ct) =>
        await EnrollmentsInWindow(ctx)
            .CountAsync(e => e.EnrolledAt >= ctx.From && e.EnrolledAt <= ctx.To, ct);

    public async Task<int> CountExitedAsync(StatisticsQuery ctx, CancellationToken ct) =>
        await EnrollmentsInWindow(ctx)
            .CountAsync(e => e.LeftAt != null && e.LeftAt >= ctx.From && e.LeftAt <= ctx.To, ct);

    public async Task<int> CountActiveOnAsync(StatisticsQuery ctx, DateOnly on, CancellationToken ct)
    {
        var classIds = ClassesInScope(ctx).Select(c => c.Id);

        return await Scope(_context.Enrollments, ctx)
            .Where(e => classIds.Contains(e.ClassId))
            .CountAsync(e => e.EnrolledAt <= on && (e.LeftAt == null || e.LeftAt > on), ct);
    }

    public async Task<IReadOnlyList<CountRow>> GetExitsByReasonAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var rows = await EnrollmentsInWindow(ctx)
            .Where(e => e.LeftAt != null && e.LeftAt >= ctx.From && e.LeftAt <= ctx.To)
            .GroupBy(e => e.ExitReason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .Select(r => new CountRow(
                r.Reason?.ToString() ?? "NotRecorded",
                r.Reason?.ToString() ?? "Not recorded",
                r.Count))
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public async Task<IReadOnlyList<CountRow>> GetByAcademicYearAsync(
        StatisticsQuery ctx, CancellationToken ct)
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

    // Id after Grade and Name: class names repeat across schools, and an unbroken tie lets
    // LIMIT/OFFSET rearrange rows between pages, dropping some and duplicating others.
    private IQueryable<ClassCapacityRow> CapacityQuery(StatisticsQuery ctx)
    {
        var enrollments = EnrollmentsInWindow(ctx).Where(e => e.LeftAt == null);

        return from c in ClassesInScope(ctx)
               orderby c.Grade, c.Name, c.Id
               select new ClassCapacityRow(
                   c.Id,
                   c.Name,
                   c.Grade,
                   c.Capacity,
                   enrollments.Count(e => e.ClassId == c.Id));
    }

    public async Task<IReadOnlyList<ClassCapacityRow>> GetCapacityAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await CapacityQuery(ctx).ToListAsync(ct);

    public Task<PagedResultDto<ClassCapacityRow>> GetCapacityPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
        PageAsync(CapacityQuery(ctx), pagination, ct);
}
