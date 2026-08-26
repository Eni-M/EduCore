using EduCore.Dtos.Statistics;
using EduCore.Data;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories.Statistics;

// Attendance rows carry no date of their own -- they hang off the diary entry for the lesson.
// Every query here therefore joins through LessonsInWindow, which is also why
// DiaryEntry(SchoolId, Date) is indexed.
public class AttendanceStatisticsRepository(AppDbContext context)
    : StatisticsRepositoryBase(context), IAttendanceStatisticsRepository
{
    private IQueryable<Models.Attendance> AttendanceInWindow(StatisticsContext ctx)
    {
        var lessonIds = LessonsInWindow(ctx).Select(d => d.Id);

        return Scope(_context.Attendances, ctx).Where(a => lessonIds.Contains(a.DiaryEntryId));
    }

    public async Task<AttendanceTotals> GetTotalsAsync(StatisticsContext ctx, CancellationToken ct)
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

    public async Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(
        StatisticsContext ctx, CancellationToken ct) =>
        await (from a in AttendanceInWindow(ctx)
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
        StatisticsContext ctx, CancellationToken ct)
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

    public async Task<IReadOnlyList<AttendanceGroup>> GetBySubjectAsync(
        StatisticsContext ctx, CancellationToken ct) =>
        await (from a in AttendanceInWindow(ctx)
               join d in LessonsInWindow(ctx) on a.DiaryEntryId equals d.Id
               join s in Scope(_context.Subjects, ctx) on d.SubjectId equals s.Id
               group a by new { s.Id, s.Name }
               into g
               orderby g.Key.Name
               select new AttendanceGroup(
                   g.Key.Id.ToString(),
                   g.Key.Name,
                   g.Count(),
                   g.Count(x => x.IsPresent),
                   g.Count(x => !x.IsPresent && x.IsExcused)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(DateOnly Day, int Records, int Present)>> GetDailyTrendAsync(
        StatisticsContext ctx, CancellationToken ct)
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
        StatisticsContext ctx, CancellationToken ct)
    {
        var rows = await StudentRowsAsync(ctx, ct);

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

    public async Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsContext ctx, double absenceRateThreshold, CancellationToken ct)
    {
        var rows = await StudentRowsAsync(ctx, ct);

        // Excused absences are counted in the total but the threshold is applied to unexcused
        // ones -- a student with a term of doctor's notes is not the same problem as a student
        // who simply stops coming.
        return rows
            .Select(r => new
            {
                Row = r,
                Unexcused = r.Records - r.Present - r.Excused
            })
            .Where(x => x.Row.Records > 0 && Rate(x.Unexcused, x.Row.Records) >= absenceRateThreshold)
            .OrderByDescending(x => Rate(x.Unexcused, x.Row.Records))
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

    private record StudentRow(
        Guid StudentId, string Name, Guid ClassId, string ClassName,
        int Records, int Present, int Excused);

    private async Task<List<StudentRow>> StudentRowsAsync(StatisticsContext ctx, CancellationToken ct)
    {
        var rows = await (from a in AttendanceInWindow(ctx)
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
                          select new
                          {
                              g.Key.Id,
                              g.Key.FirstName,
                              g.Key.LastName,
                              g.Key.ClassId,
                              g.Key.ClassName,
                              Records = g.Count(),
                              Present = g.Count(x => x.IsPresent),
                              Excused = g.Count(x => !x.IsPresent && x.IsExcused)
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => new StudentRow(
                r.Id,
                r.FirstName + " " + r.LastName,
                r.ClassId,
                r.ClassName,
                r.Records,
                r.Present,
                r.Excused))
            .ToList();
    }

    private static double Rate(int part, int total) =>
        total == 0 ? 0 : Math.Round(part * 100.0 / total, 2);
}
