using EduCore.Helpers;
using EduCore.Repositories.Interfaces;
using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class StatisticsOverviewService(
    IStudentStatisticsService students,
    IEnrollmentStatisticsService enrollment,
    IAttendanceStatisticsService attendance,
    IAcademicStatisticsService academic,
    ITeacherStatisticsService teachers,
    IClassStatisticsService classes,
    IStatisticsSnapshotService snapshotService,
    ICellSuppressor suppressor,
    IStatisticsScopeResolver scope,
    ISchoolRepository schools) : IStatisticsOverviewService
{
    // No pagination params -> the whole class list (chart/export callers).
    public Task<SchoolOverviewDto> GetSchoolOverviewAsync(StatisticsFilter filter, CancellationToken ct) =>
        BuildAsync(filter, pagination: null, ct);

    public Task<SchoolOverviewDto> GetSchoolOverviewAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct) =>
        BuildAsync(filter, pagination, ct);

    private async Task<SchoolOverviewDto> BuildAsync(
        StatisticsFilter filter, PaginationQueryDto? pagination, CancellationToken ct)
    {
        // Paged requests aggregate only the requested page of classes in the database.
        // The unpaged path (chart/export callers) keeps the previous full-list behavior.
        var classesPage = pagination is null
            ? await FullClassListAsync(filter, ct)
            : await classes.GetSummariesPagedAsync(filter, pagination, ct);

        var ctx = await scope.ResolveAsync(filter, ct);
        var snapshot = await snapshotService.GetAsync(ctx, ct);

        // A snapshot answers all five summary blocks from one row. Without one — because the
        // caller narrowed the query, or the feature is off — fall through to the services,
        // which recompute the same figures live.
        var (studentSummary, enrollmentSummary, attendanceSummary, academicSummary, teacherSummary) =
            snapshot is not null
                ? (SnapshotProjection.Students(snapshot),
                    SnapshotProjection.Enrollment(snapshot),
                    SnapshotProjection.Attendance(snapshot, suppressor),
                    SnapshotProjection.Academic(snapshot, suppressor),
                    SnapshotProjection.Teachers(snapshot, suppressor))
                : (await students.GetSummaryAsync(filter, ct),
                    await enrollment.GetSummaryAsync(filter, ct),
                    await attendance.GetSummaryAsync(filter, ct),
                    await academic.GetSummaryAsync(filter, ct),
                    await teachers.GetSummaryAsync(filter, ct));

        return new SchoolOverviewDto(
            studentSummary,
            enrollmentSummary,
            attendanceSummary,
            academicSummary,
            teacherSummary,
            classesPage.Items,
            classesPage.Page,
            classesPage.PageSize,
            classesPage.TotalCount);
    }

    private async Task<PagedResultDto<ClassSummaryDto>> FullClassListAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var allClasses = await classes.GetSummariesAsync(filter, ct);

        return new PagedResultDto<ClassSummaryDto>(
            allClasses, 1, allClasses.Count == 0 ? 1 : allClasses.Count, allClasses.Count);
    }

    private async Task<PagedResultDto<Models.School>> FullSchoolListAsync(StatisticsQuery ctx, CancellationToken ct)
    {
        var all = (await schools.GetAllOrderedAsync(activeOnly: false, ct))
            .Where(s => ctx.CityId is null || s.CityId == ctx.CityId)
            .Where(s => ctx.SchoolStateId is null || s.City?.StateId == ctx.SchoolStateId)
            .ToList();

        return new PagedResultDto<Models.School>(all, 1, all.Count == 0 ? 1 : all.Count, all.Count);
    }

    private async Task<PagedResultDto<Models.School>> ScopedSchoolPageAsync(
        StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct)
    {
        if (ctx.CityId is null && ctx.SchoolStateId is null)
            return await schools.GetOrderedPagedAsync(activeOnly: false, pagination, ct);

        var (items, total) = await schools.GetPagedAsync(
            activeOnly: false, ctx.CityId, ctx.SchoolStateId, pagination, search: null, ct);
        var (page, pageSize) = ListPagination.Normalize(pagination);
        return new PagedResultDto<Models.School>(items, page, pageSize, total);
    }

    public async Task<PlatformOverviewDto> GetPlatformOverviewAsync(
        StatisticsFilter filter, PaginationQueryDto? pagination, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        // The comparison table is one row per school and the country has many, so it is paged
        // in the database rather than built for every school and then trimmed.
        // The school list follows the same scope as the figures: a regional admin's city, or the
        // region a ministry user narrowed to. Without it a region counted every school nationwide.
        var page = pagination is null
            ? await FullSchoolListAsync(ctx, ct)
            : await ScopedSchoolPageAsync(ctx, pagination, ct);

        var schoolIds = page.Items.Select(s => s.Id).ToList();

        // One query for the whole page's figures. This replaced a loop that ran five statistics
        // services per school — roughly 2,500 sequential statements for 80 schools, each one a
        // separate round trip to the database.
        var bySchoolSnapshot = await snapshotService.GetManyAsync(ctx, schoolIds, ct);

        var bySchool = new List<SchoolComparisonDto>();

        foreach (var school in page.Items)
        {
            if (bySchoolSnapshot.TryGetValue(school.Id, out var snapshot))
            {
                var schoolAcademic = SnapshotProjection.Academic(snapshot, suppressor);

                bySchool.Add(new SchoolComparisonDto(
                    school.Id,
                    school.Name,
                    school.Type,
                    snapshot.StudentsActive,
                    snapshot.Teachers,
                    snapshot.Classes,
                    SnapshotProjection.Attendance(snapshot, suppressor).AttendanceRate,
                    schoolAcademic.AverageFinalMark,
                    schoolAcademic.PassRate));

                continue;
            }

            // Snapshots switched off: compute this school the old way.
            var scoped = filter with { SchoolId = school.Id };

            var schoolStudents = await students.GetSummaryAsync(scoped, ct);
            var schoolTeachers = await teachers.GetSummaryAsync(scoped, ct);
            var schoolAttendance = await attendance.GetSummaryAsync(scoped, ct);
            var liveAcademic = await academic.GetSummaryAsync(scoped, ct);
            var schoolClasses = await classes.GetSummariesAsync(scoped, ct);

            bySchool.Add(new SchoolComparisonDto(
                school.Id,
                school.Name,
                school.Type,
                schoolStudents.Active,
                schoolTeachers.Teachers,
                schoolClasses.Count,
                schoolAttendance.AttendanceRate,
                liveAcademic.AverageFinalMark,
                liveAcademic.PassRate));
        }

        return new PlatformOverviewDto(
            page.TotalCount,
            await students.GetSummaryAsync(filter, ct),
            await attendance.GetSummaryAsync(filter, ct),
            await academic.GetSummaryAsync(filter, ct),
            await teachers.GetSummaryAsync(filter, ct),
            bySchool,
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}
