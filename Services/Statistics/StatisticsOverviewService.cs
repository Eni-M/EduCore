using EduCore.Data;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services.Statistics;

// The two dashboard endpoints. These compose the focused services and define no metric of
// their own, so nothing here can drift from what the granular endpoints report. Neither
// returns a record that names an individual.
public class StatisticsOverviewService(
    IStudentStatisticsService students,
    IEnrollmentStatisticsService enrollment,
    IAttendanceStatisticsService attendance,
    IAcademicStatisticsService academic,
    ITeacherStatisticsService teachers,
    IClassStatisticsService classes,
    IStatisticsScopeResolver scope,
    AppDbContext context) : IStatisticsOverviewService
{
    public async Task<SchoolOverviewDto> GetSchoolOverviewAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        new(
            await students.GetSummaryAsync(filter, ct),
            await enrollment.GetSummaryAsync(filter, ct),
            await attendance.GetSummaryAsync(filter, ct),
            await academic.GetSummaryAsync(filter, ct),
            await teachers.GetSummaryAsync(filter, ct),
            await classes.GetSummariesAsync(filter, ct));

    // Supersedes the old GET /api/schools/statistics, which returned nine bare counts with no
    // period, no context and no per-school breakdown of outcomes.
    public async Task<PlatformOverviewDto> GetPlatformOverviewAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        // Resolved once here purely to enforce the caller's access before the per-school loop
        // below re-resolves for each school.
        _ = await scope.ResolveAsync(filter, ct);

        var schools = await context.Schools
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Type })
            .ToListAsync(ct);

        var bySchool = new List<SchoolComparisonDto>();

        foreach (var school in schools)
        {
            var scoped = filter with { SchoolId = school.Id };

            var schoolStudents = await students.GetSummaryAsync(scoped, ct);
            var schoolTeachers = await teachers.GetSummaryAsync(scoped, ct);
            var schoolAttendance = await attendance.GetSummaryAsync(scoped, ct);
            var schoolAcademic = await academic.GetSummaryAsync(scoped, ct);
            var schoolClasses = await classes.GetSummariesAsync(scoped, ct);

            bySchool.Add(new SchoolComparisonDto(
                school.Id,
                school.Name,
                school.Type,
                schoolStudents.Active,
                schoolTeachers.Teachers,
                schoolClasses.Count,
                schoolAttendance.AttendanceRate,
                schoolAcademic.AverageFinalMark,
                schoolAcademic.PassRate));
        }

        return new PlatformOverviewDto(
            schools.Count,
            await students.GetSummaryAsync(filter, ct),
            await attendance.GetSummaryAsync(filter, ct),
            await academic.GetSummaryAsync(filter, ct),
            await teachers.GetSummaryAsync(filter, ct),
            bySchool);
    }
}
