using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Data;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Helpers;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

public class AttendanceStatisticsRepository(AppDbContext context)
    : StatisticsRepositoryBase(context), IAttendanceStatisticsRepository
{
    private IQueryable<Models.Attendance> AttendanceInWindow(StatisticsQuery ctx)
    {
        var lessonIds = LessonsInWindow(ctx).Select(d => d.Id);

        return Scope(_context.Attendances, ctx).Where(a => lessonIds.Contains(a.DiaryEntryId));
    }

    public async Task<AttendanceTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct)
    {
        var attendance = AttendanceInWindow(ctx);
        var records = await attendance.CountAsync(ct);

        if (records == 0)
            return new AttendanceTotals(0, 0, 0, 0, null, 0);

        var behaviorMarks = attendance.Where(a => a.BehaviorMark != null);
        var behaviorMarkCount = await behaviorMarks.CountAsync(ct);

        return new AttendanceTotals(
            records,
            await attendance.CountAsync(a => a.IsPresent, ct),
            await attendance.CountAsync(a => !a.IsPresent, ct),
            await attendance.CountAsync(a => !a.IsPresent && a.IsExcused, ct),
            behaviorMarkCount == 0
                ? null
                : await behaviorMarks.AverageAsync(a => a.BehaviorMark!.Value, ct),
            behaviorMarkCount);
    }

    // Name then Id: class and subject names are not unique across schools, and LIMIT/OFFSET
    // over a tie is free to reorder rows between pages, hiding some and repeating others.
    private IQueryable<AttendanceGroup> ByClassQuery(StatisticsQuery ctx) =>
        from a in AttendanceInWindow(ctx)
        join c in ClassesInScope(ctx) on a.ClassId equals c.Id
        group a by new { c.Id, c.Name }
        into g
        orderby g.Key.Name, g.Key.Id
        select new AttendanceGroup(
            g.Key.Id.ToString(),
            g.Key.Name,
            g.Count(),
            g.Count(x => x.IsPresent),
            g.Count(x => !x.IsPresent && x.IsExcused));

    public async Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await ByClassQuery(ctx).ToListAsync(ct);

    public Task<PagedResultDto<AttendanceGroup>> GetByClassPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
        PageAsync(ByClassQuery(ctx), pagination, ct);

    public Task<PagedResultDto<AttendanceGroup>> GetBySubjectPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
        PageAsync(BySubjectQuery(ctx), pagination, ct);

    public async Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct) =>
        await (from a in AttendanceInWindow(ctx).Where(a => classIds.Contains(a.ClassId))
               join c in ClassesInScope(ctx) on a.ClassId equals c.Id
               group a by new { c.Id, c.Name }
               into g
               orderby g.Key.Name
               select new AttendanceGroup(
                   g.Key.Id.ToString(),
                   g.Key.Name,
                   g.Count(),
                   g.Count(x => x.IsPresent),
                   g.Count(x => !x.IsPresent && x.IsExcused)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AttendanceGroup>> GetByGradeAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var rows = await (from a in AttendanceInWindow(ctx)
                          join c in ClassesInScope(ctx) on a.ClassId equals c.Id
                          group a by c.Grade
                          into g
                          orderby g.Key
                          select new
                          {
                              Grade = g.Key,
                              Records = g.Count(),
                              Present = g.Count(x => x.IsPresent),
                              Excused = g.Count(x => !x.IsPresent && x.IsExcused)
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => new AttendanceGroup(
                r.Grade.ToString(), "Grade " + r.Grade, r.Records, r.Present, r.Excused))
            .ToList();
    }

    private IQueryable<AttendanceGroup> BySubjectQuery(StatisticsQuery ctx) =>
        from a in AttendanceInWindow(ctx)
        join d in LessonsInWindow(ctx) on a.DiaryEntryId equals d.Id
        join s in Scope(_context.Subjects, ctx) on d.SubjectId equals s.Id
        group a by new { s.Id, s.Name }
        into g
        orderby g.Key.Name, g.Key.Id
        select new AttendanceGroup(
            g.Key.Id.ToString(),
            g.Key.Name,
            g.Count(),
            g.Count(x => x.IsPresent),
            g.Count(x => !x.IsPresent && x.IsExcused));

    public async Task<IReadOnlyList<AttendanceGroup>> GetBySubjectAsync(
        StatisticsQuery ctx, CancellationToken ct) =>
        await BySubjectQuery(ctx).ToListAsync(ct);

    public async Task<IReadOnlyList<(DateOnly Day, int Records, int Present)>> GetDailyTrendAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var rows = await (from a in AttendanceInWindow(ctx)
                          join d in LessonsInWindow(ctx) on a.DiaryEntryId equals d.Id
                          group a by d.Date
                          into g
                          orderby g.Key
                          select new
                          {
                              Day = g.Key,
                              Records = g.Count(),
                              Present = g.Count(x => x.IsPresent)
                          })
            .ToListAsync(ct);

        return rows.Select(r => (r.Day, r.Records, r.Present)).ToList();
    }

    public async Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var rows = await StudentAttendanceRowsAsync(ctx, ct);

        return rows
            .OrderBy(r => r.Name)
            .Select(r => new StudentAttendanceSummaryDto(
                r.StudentId,
                r.Name,
                r.ClassId,
                r.ClassName,
                r.Records,
                r.Present,
                r.Records - r.Present,
                r.Excused,
                Rate(r.Present, r.Records)))
            .ToList();
    }

    public async Task<PagedResultDto<StudentAttendanceSummaryDto>> GetByStudentPagedAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        // Same rows as GetByStudentAsync, but grouped, ordered and sliced in the database.
        // The endpoint used to load every student in scope and then throw away all but 20 —
        // the payload shrank, the read did not.
        var matches = StudentAttendanceAggregates(ctx);

        var totalCount = await matches.CountAsync(ct);

        var items = await matches
            // Name preserves the previous list order. Student and class break ties, because
            // two students sharing a name would otherwise be free to swap places between two
            // LIMIT/OFFSET queries, showing one twice and the other never.
            .OrderBy(r => r.Name)
            .ThenBy(r => r.StudentId)
            .ThenBy(r => r.ClassId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<StudentAttendanceSummaryDto>(
            items.Select(r => new StudentAttendanceSummaryDto(
                r.StudentId,
                r.Name,
                r.ClassId,
                r.ClassName,
                r.Records,
                r.Present,
                r.Records - r.Present,
                r.Excused,
                Rate(r.Present, r.Records))).ToList(),
            page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsQuery ctx, double absenceRateThreshold, CancellationToken ct)
    {
        var rows = await StudentAttendanceRowsAsync(ctx, ct);

        return rows
            .Select(r => new
            {
                Row = r,
                Unexcused = r.Records - r.Present - r.Excused
            })
            .Where(x => x.Row.Records > 0 && x.Unexcused * 100.0 >= absenceRateThreshold * x.Row.Records)
            .OrderByDescending(x => x.Unexcused * 100.0 / x.Row.Records)
            .ThenBy(x => x.Row.StudentId)
            .ThenBy(x => x.Row.ClassId)
            .Select(x => new ExcessiveAbsenceStudentDto(
                x.Row.StudentId,
                x.Row.Name,
                x.Row.ClassId,
                x.Row.ClassName,
                x.Row.Records,
                x.Row.Records - x.Row.Present,
                x.Row.Excused,
                Rate(x.Row.Records - x.Row.Present, x.Row.Records)))
            .ToList();
    }

    private async Task<List<StudentAttendanceRow>> StudentAttendanceRowsAsync(StatisticsQuery ctx, CancellationToken ct) =>
        await StudentAttendanceAggregates(ctx).ToListAsync(ct);

    public async Task<PagedResultDto<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesPagedAsync(
        StatisticsQuery ctx,
        double absenceRateThreshold,
        PaginationQueryDto pagination,
        CancellationToken ct)
    {
        var (page, pageSize) = ListPagination.Normalize(pagination);

        // Same per-student rows and rule as GetExcessiveAbsencesAsync via the shared builder,
        // tie-broken by student then class so LIMIT/OFFSET pages are deterministic. PostgreSQL
        // has no round(double, int), so the threshold is compared multiplied out rather than
        // through Math.Round, in both paths. Note the two different rates, preserved here: the
        // filter uses the *unexcused* share while the DTO reports the overall absence share.
        var matches = StudentAttendanceAggregates(ctx)
            .Where(s => s.Records > 0
                && (s.Records - s.Present - s.Excused) * 100.0 >= absenceRateThreshold * s.Records);

        var totalCount = await matches.CountAsync(ct);

        var items = await matches
            .OrderByDescending(s => (s.Records - s.Present - s.Excused) * 100.0 / s.Records)
            .ThenBy(s => s.StudentId)
            .ThenBy(s => s.ClassId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResultDto<ExcessiveAbsenceStudentDto>(
            items.Select(r => new ExcessiveAbsenceStudentDto(
                r.StudentId,
                r.Name,
                r.ClassId,
                r.ClassName,
                r.Records,
                r.Records - r.Present,
                r.Excused,
                Rate(r.Records - r.Present, r.Records))).ToList(),
            page, pageSize, totalCount);
    }

    private IQueryable<StudentAttendanceRow> StudentAttendanceAggregates(StatisticsQuery ctx) =>
        from a in AttendanceInWindow(ctx)
        join e in Scope(_context.Enrollments, ctx) on a.EnrollmentId equals e.Id
        join st in Scope(_context.Students, ctx) on e.StudentId equals st.Id
        join u in _context.Users on st.UserId equals u.Id
        join c in ClassesInScope(ctx) on a.ClassId equals c.Id
        group a by new
        {
            st.Id,
            u.FirstName,
            u.LastName,
            ClassId = c.Id,
            ClassName = c.Name
        }
        into g
        select new StudentAttendanceRow
        {
            StudentId = g.Key.Id,
            Name = g.Key.FirstName + " " + g.Key.LastName,
            ClassId = g.Key.ClassId,
            ClassName = g.Key.ClassName,
            Records = g.Count(),
            Present = g.Count(x => x.IsPresent),
            Excused = g.Count(x => !x.IsPresent && x.IsExcused)
        };

    private static double Rate(int part, int total) =>
        total == 0 ? 0 : Math.Round(part * 100.0 / total, 2);
}
