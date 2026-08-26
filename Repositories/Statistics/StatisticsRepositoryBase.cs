using EduCore.Dtos.Statistics;
using EduCore.Data;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

// Every statistics query goes through Scope, which is the one place school isolation is
// applied for this module.
//
// The rest of the app relies on the global query filter in AppDbContext. Statistics cannot:
// a SuperAdmin has no school of their own, so the filter would match nothing for them. These
// queries therefore drop the filter and scope explicitly -- and the guard below makes running
// unscoped an exception rather than a silent leak of every school's data.
public abstract class StatisticsRepositoryBase(AppDbContext context)
{
    protected readonly AppDbContext _context = context;

    protected IQueryable<T> Scope<T>(IQueryable<T> query, StatisticsContext ctx)
        where T : class, ISchoolScoped
    {
        if (ctx.SchoolId is null && !ctx.CrossSchool)
            throw new InvalidOperationException(
                "A statistics query reached the database without a school scope. " +
                "StatisticsScopeResolver must produce either a school id or an explicit cross-school context.");

        query = query.AsNoTracking().IgnoreQueryFilters();

        return ctx.SchoolId is null
            ? query
            : query.Where(e => e.SchoolId == ctx.SchoolId);
    }

    // Attendance and diary entries are dated by the lesson, so a calendar window is applied
    // to the diary entry rather than to the attendance row.
    protected IQueryable<Models.DiaryEntry> LessonsInWindow(StatisticsContext ctx)
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

    // Marks belong to a grading period rather than to a date, so they are windowed by the
    // periods that overlap the requested range.
    protected IQueryable<Models.Mark> MarksInWindow(StatisticsContext ctx)
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

    protected IQueryable<Models.Class> ClassesInScope(StatisticsContext ctx)
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

    // Enrollments that were live at any point in the window.
    protected IQueryable<Models.Enrollment> EnrollmentsInWindow(StatisticsContext ctx)
    {
        var classIds = ClassesInScope(ctx).Select(c => c.Id);

        return Scope(_context.Enrollments, ctx)
            .Where(e => classIds.Contains(e.ClassId))
            .Where(e => e.EnrolledAt <= ctx.To && (e.LeftAt == null || e.LeftAt >= ctx.From));
    }
}
