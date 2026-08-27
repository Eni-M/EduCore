using EduCore.Dtos.Statistics;
using EduCore.Grading;
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

    // Assignments to classes that are in scope, active at some point in the window. Teacher
    // has no employment dates and no academic year of its own, so this is what ties a teacher
    // to a period: the class they are assigned to belongs to one academic year, and
    // ClassesInScope applies the year/class/grade filters.
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

        // Asking about one named teacher is a different question from "who taught here this
        // year": their detail view must still render for a period they did not teach in,
        // showing zeros rather than a missing row.
        if (ctx.TeacherId is not null)
            return teachers.Where(t => t.Id == ctx.TeacherId);

        // Otherwise "teachers" means the ones actually teaching in this period. Counting every
        // teacher row in the school instead would report the same figure for every academic
        // year, and would credit a newly hired teacher to years they were never here for.
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

    public async Task<IReadOnlyList<TeacherWorkloadDto>> GetWorkloadAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var assignments = AssignmentsInWindow(ctx);
        var lessons = LessonsInWindow(ctx);
        var activeEnrollments = EnrollmentsInWindow(ctx).Where(e => e.LeftAt == null);

        var rows = await (from t in TeachersInScope(ctx)
                          join u in _context.Users on t.UserId equals u.Id
                          orderby u.LastName, u.FirstName
                          select new
                          {
                              t.Id,
                              u.FirstName,
                              u.LastName,
                              Classes = assignments
                                  .Where(a => a.TeacherId == t.Id)
                                  .Select(a => a.ClassId)
                                  .Distinct()
                                  .Count(),
                              Subjects = assignments
                                  .Where(a => a.TeacherId == t.Id)
                                  .Select(a => a.SubjectId)
                                  .Distinct()
                                  .Count(),
                              Students = activeEnrollments
                                  .Where(e => assignments
                                      .Where(a => a.TeacherId == t.Id)
                                      .Select(a => a.ClassId)
                                      .Contains(e.ClassId))
                                  .Select(e => e.StudentId)
                                  .Distinct()
                                  .Count(),
                              Lessons = lessons.Count(d => d.TeacherId == t.Id)
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => new TeacherWorkloadDto(
                r.Id, r.FirstName + " " + r.LastName, r.Classes, r.Subjects, r.Students, r.Lessons))
            .ToList();
    }

    public async Task<IReadOnlyList<TeacherCoverageDto>> GetCoverageAsync(
        StatisticsQuery ctx, CancellationToken ct)
    {
        var lessons = LessonsInWindow(ctx);
        var attendances = Scope(_context.Attendances, ctx);
        var activeEnrollments = EnrollmentsInWindow(ctx).Where(e => e.LeftAt == null);
        var periodMarks = MarksInWindow(ctx);

        var rows = await (from t in TeachersInScope(ctx)
                          join u in _context.Users on t.UserId equals u.Id
                          orderby u.LastName, u.FirstName
                          select new
                          {
                              t.Id,
                              u.FirstName,
                              u.LastName,
                              Lessons = lessons.Count(d => d.TeacherId == t.Id),

                              // A register counts as incomplete when it holds fewer rows than
                              // the class had enrolled students at the time.
                              Incomplete = lessons
                                  .Where(d => d.TeacherId == t.Id)
                                  .Count(d => attendances.Count(a => a.DiaryEntryId == d.Id)
                                              < activeEnrollments.Count(e => e.ClassId == d.ClassId)),

                              MarksTotal = periodMarks.Count(m => m.TeacherId == t.Id),
                              MarksComplete = periodMarks.Count(m => m.TeacherId == t.Id
                                                                     && m.FirstMark != null
                                                                     && m.SecondMark != null)
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => new TeacherCoverageDto(
                r.Id,
                r.FirstName + " " + r.LastName,
                r.Lessons,
                r.Incomplete,
                r.MarksTotal,
                r.MarksComplete,
                r.MarksTotal - r.MarksComplete,
                r.MarksTotal == 0 ? 0 : Math.Round(r.MarksComplete * 100.0 / r.MarksTotal, 2)))
            .ToList();
    }

    public async Task<IReadOnlyList<(Guid ClassId, string ClassName, int Grade, Guid SubjectId,
        string SubjectName, int GradedMarks, double? Average, int Passed)>> GetPerformanceAsync(
        StatisticsQuery ctx, Guid teacherId, CancellationToken ct)
    {
        var pass = grading.PassValue;

        // Inline for the same reason as AcademicStatisticsRepository: a constructed type
        // cannot be joined or grouped on in a translatable query.
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
