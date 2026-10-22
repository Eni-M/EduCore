using EduCore.Dtos.Pagination;
using EduCore.Helpers;
using EduCore.Dtos.Statistics;
using EduCore.Data;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public abstract class StatisticsRepositoryBase(AppDbContext context)
{
    protected readonly AppDbContext _context = context;

    protected IQueryable<T> Scope<T>(IQueryable<T> query, StatisticsQuery ctx)
        where T : class, ISchoolScoped
    {
        if (ctx.SchoolId is null && !ctx.CrossSchool)
            throw new InvalidOperationException(
                "A statistics query reached the database without a school scope. " +
                "StatisticsScopeResolver must produce either a school id or an explicit cross-school context.");

        query = query.AsNoTracking().IgnoreQueryFilters(QueryFilters.SchoolOnly);

        if (ctx.SchoolId is not null)
            query = query.Where(e => e.SchoolId == ctx.SchoolId);

        if (ctx.CityId is not null || ctx.ZoneId is not null || ctx.SchoolStateId is not null)
        {
            var schoolIds = _context.Schools
                .AsNoTracking()
                .Where(s => ctx.CityId == null || s.CityId == ctx.CityId)
                .Where(s => ctx.ZoneId == null || s.ZoneId == ctx.ZoneId)
                .Where(s => ctx.SchoolStateId == null
                            || (s.City != null && s.City.StateId == ctx.SchoolStateId))
                .Select(s => s.Id);

            query = query.Where(e => schoolIds.Contains(e.SchoolId));
        }

        return query;
    }

    // Counts and slices an already-ordered aggregate query in the database.
    //
    //
    // The query must carry its own ORDER BY, and that order must be total — every caller
    // here breaks ties on an id. LIMIT/OFFSET over a partial order is free to return rows
    // in a different arrangement for each page, which shows some rows twice and hides
    // others entirely.
    protected static async Task<PagedResultDto<T>> PageAsync<T>(
        IQueryable<T> ordered, PaginationQueryDto pagination, CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        var totalCount = await ordered.CountAsync(ct);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<T>(items, page, pageSize, totalCount);
    }

    protected IQueryable<Models.DiaryEntry> LessonsInWindow(StatisticsQuery ctx)
    {
        var lessons = Scope(_context.DiaryEntries, ctx)
            .Where(d => d.Date >= ctx.From && d.Date <= ctx.To);

        if (ctx.SubjectId is not null)
            lessons = lessons.Where(d => d.SubjectId == ctx.SubjectId);

        if (ctx.TeacherId is not null)
            lessons = lessons.Where(d => d.TeacherId == ctx.TeacherId);

        if (ctx.ClassId is not null)
            lessons = lessons.Where(d => d.ClassId == ctx.ClassId);

        return lessons;
    }

    protected IQueryable<Models.Mark> MarksInWindow(StatisticsQuery ctx)
    {
        var periodIds = Scope(_context.GradingPeriods, ctx)
            .Where(g => g.StartDate <= ctx.To && ctx.From <= g.EndDate)
            .Select(g => g.Id);

        var marks = Scope(_context.Marks, ctx).Where(m => periodIds.Contains(m.GradingPeriodId));

        if (ctx.GradingPeriodId is not null)
            marks = marks.Where(m => m.GradingPeriodId == ctx.GradingPeriodId);

        if (ctx.AcademicYearId is not null)
            marks = marks.Where(m => m.AcademicYearId == ctx.AcademicYearId);

        if (ctx.SubjectId is not null)
            marks = marks.Where(m => m.SubjectId == ctx.SubjectId);

        if (ctx.TeacherId is not null)
            marks = marks.Where(m => m.TeacherId == ctx.TeacherId);

        if (ctx.ClassId is not null)
            marks = marks.Where(m => m.ClassId == ctx.ClassId);

        return marks;
    }

    protected IQueryable<Models.Class> ClassesInScope(StatisticsQuery ctx)
    {
        var classes = Scope(_context.Classes, ctx);

        if (ctx.ClassId is not null)
            classes = classes.Where(c => c.Id == ctx.ClassId);

        if (ctx.Grade is not null)
            classes = classes.Where(c => c.Grade == ctx.Grade);

        if (ctx.AcademicYearId is not null)
            classes = classes.Where(c => c.AcademicYearId == ctx.AcademicYearId);

        return classes;
    }

    protected IQueryable<Models.Enrollment> EnrollmentsInWindow(StatisticsQuery ctx)
    {
        var enrollments = Scope(_context.Enrollments, ctx)
            .Where(e => e.EnrolledAt <= ctx.To && (e.LeftAt == null || e.LeftAt >= ctx.From));

        // The class restriction only filters when the caller narrowed to specific classes.
        // Otherwise every in-scope enrollment already belongs to an in-scope class (same
        // SchoolId through the (ClassId, SchoolId) foreign key), so an unfiltered
        // IN (SELECT Id FROM Classes) would only add a full table scan. Callers that
        // aggregate per class join ClassesInScope themselves.
        if (ctx.ClassId is not null || ctx.Grade is not null || ctx.AcademicYearId is not null)
        {
            var classIds = ClassesInScope(ctx).Select(c => c.Id);
            enrollments = enrollments.Where(e => classIds.Contains(e.ClassId));
        }

        return enrollments;
    }
}
