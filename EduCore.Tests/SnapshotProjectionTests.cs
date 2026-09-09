using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Models;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The snapshot table is only safe if a figure read from it is indistinguishable from the same
// figure computed live. These tests hold the two paths against each other for identical inputs.
// If they ever disagree, an overview screen silently reports different numbers depending on how
// recently somebody enrolled a student, which is worse than the slow query it replaced.
public class SnapshotProjectionTests
{
    private static readonly StatisticsQuery Ctx = new(
        Guid.NewGuid(), false, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
        null, null, null, null, null, null, null, null, null, null, null);

    private static readonly StudentStatisticsDto StudentTotals = new(500, 480, 20, 35, 240, 235, 5);
    private static readonly AttendanceTotals AttendanceTotals = new(1000, 940, 60, 22, 3.4, 700);
    private static readonly AcademicTotals AcademicTotals = new(800, 15, 3.62, 705, 95, 60);

    private static readonly IReadOnlyList<TeacherWorkloadDto> Workload =
    [
        new(Guid.NewGuid(), "A Teacher", Guid.NewGuid(), "School", 3, 2, 60, 40),
        new(Guid.NewGuid(), "B Teacher", Guid.NewGuid(), "School", 4, 3, 80, 55),
        new(Guid.NewGuid(), "C Teacher", Guid.NewGuid(), "School", 2, 1, 30, 20),
    ];

    // The real suppressor, not a pass-through: suppression is exactly the kind of rule that
    // could be applied on one path and forgotten on the other.
    private static ICellSuppressor Suppressor(int minimumCellSize = 5) =>
        new CellSuppressor(Options.Create(new PrivacyOptions { MinimumCellSize = minimumCellSize }));

    private static SchoolStatisticsSnapshot SnapshotOf(int teachers = 25) => new()
    {
        SchoolId = Ctx.SchoolId!.Value,
        From = Ctx.From,
        To = Ctx.To,

        StudentsTotal = StudentTotals.Total,
        StudentsActive = StudentTotals.Active,
        StudentsGraduated = StudentTotals.Graduated,
        StudentsNewArrivals = StudentTotals.NewArrivals,
        StudentsFemale = StudentTotals.Female,
        StudentsMale = StudentTotals.Male,
        StudentsGenderNotRecorded = StudentTotals.GenderNotRecorded,

        EnrollmentsActive = 480,
        EnrollmentsStarted = 35,
        EnrollmentsExited = 12,
        EnrollmentsActiveYearAgo = 450,

        AttendanceRecords = AttendanceTotals.Records,
        AttendancePresent = AttendanceTotals.Present,
        AttendanceAbsent = AttendanceTotals.Absent,
        AttendanceExcusedAbsences = AttendanceTotals.ExcusedAbsences,
        AttendanceAverageBehaviorMark = AttendanceTotals.AverageBehaviorMark,
        AttendanceBehaviorMarkCount = AttendanceTotals.BehaviorMarkCount,

        AcademicGradedMarks = AcademicTotals.GradedMarks,
        AcademicIncompleteMarks = AcademicTotals.IncompleteMarks,
        AcademicAverageFinalMark = AcademicTotals.AverageFinalMark,
        AcademicPassed = AcademicTotals.Passed,
        AcademicFailed = AcademicTotals.Failed,
        AcademicAtRisk = AcademicTotals.AtRisk,

        Teachers = teachers,
        Classes = 20,
        TeachersWithWorkload = Workload.Count,
        WorkloadClassesSum = Workload.Sum(w => w.Classes),
    };

    [Fact]
    public async Task Attendance_read_from_a_snapshot_equals_attendance_computed_live()
    {
        var suppressor = Suppressor();

        var live = await new AttendanceStatisticsService(
                new StubAttendance(), new StubScope(), suppressor)
            .GetSummaryAsync(new StatisticsFilter(), CancellationToken.None);

        var projected = SnapshotProjection.Attendance(SnapshotOf(), suppressor);

        Assert.Equal(live, projected);
    }

    [Fact]
    public async Task Academic_read_from_a_snapshot_equals_academic_computed_live()
    {
        var suppressor = Suppressor();

        var live = await new AcademicStatisticsService(
                new StubAcademic(), new StubScope(), suppressor)
            .GetSummaryAsync(new StatisticsFilter(), CancellationToken.None);

        var projected = SnapshotProjection.Academic(SnapshotOf(), suppressor);

        Assert.Equal(live, projected);
    }

    [Fact]
    public async Task Teacher_ratios_read_from_a_snapshot_equal_the_ones_computed_live()
    {
        var suppressor = Suppressor();

        var live = await new TeacherStatisticsService(
                new StubTeachers(), new StubAcademic(), new StubStudents(), new StubScope(), suppressor)
            .GetSummaryAsync(new StatisticsFilter(), CancellationToken.None);

        var projected = SnapshotProjection.Teachers(SnapshotOf(), suppressor);

        // Averages in particular: the snapshot stores the sum and the count rather than the
        // average, precisely so this rounds once here instead of twice.
        Assert.Equal(live, projected);
    }

    [Fact]
    public async Task Enrollment_read_from_a_snapshot_equals_enrollment_computed_live()
    {
        var live = await new EnrollmentStatisticsService(new StubEnrollments(), new StubScope())
            .GetSummaryAsync(new StatisticsFilter(), CancellationToken.None);

        var projected = SnapshotProjection.Enrollment(SnapshotOf());

        Assert.Equal(live, projected);
    }

    [Fact]
    public void Suppression_is_applied_on_the_snapshot_path_too()
    {
        // A cell of 3 marks is below a minimum of 5, so its value must be withheld. If the
        // snapshot path skipped suppression it would leak exactly the small groups the
        // privacy threshold exists to protect.
        var snapshot = SnapshotOf();
        snapshot.AcademicGradedMarks = 3;
        snapshot.AcademicPassed = 2;
        snapshot.AcademicFailed = 1;

        var projected = SnapshotProjection.Academic(snapshot, Suppressor(minimumCellSize: 5));

        Assert.True(projected.PassRate.Suppressed);
        Assert.Null(projected.PassRate.Value);
    }

    [Fact]
    public void Raising_the_privacy_threshold_takes_effect_without_recomputing_stored_rows()
    {
        // The snapshot stores counts, not presented figures, so a change to the threshold
        // applies on the next read rather than needing every row rebuilt.
        var snapshot = SnapshotOf();
        snapshot.AttendanceRecords = 10;
        snapshot.AttendancePresent = 9;

        Assert.False(SnapshotProjection.Attendance(snapshot, Suppressor(5)).AttendanceRate.Suppressed);
        Assert.True(SnapshotProjection.Attendance(snapshot, Suppressor(50)).AttendanceRate.Suppressed);
    }

    // --- stubs: the members the services actually call return the shared fixtures above ---

    private sealed class StubScope : IStatisticsScopeResolver
    {
        public Task<StatisticsQuery> ResolveAsync(StatisticsFilter filter, CancellationToken ct) =>
            Task.FromResult(Ctx);
    }

    private sealed class StubAttendance : IAttendanceStatisticsRepository
    {
        public Task<AttendanceTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct) =>
            Task.FromResult(AttendanceTotals);

        public Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(
            StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AttendanceGroup>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AttendanceGroup>> GetBySubjectAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<AttendanceGroup>> GetByClassPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<AttendanceGroup>> GetBySubjectPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<(DateOnly Day, int Records, int Present)>> GetDailyTrendAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<PagedResultDto<StudentAttendanceSummaryDto>> GetByStudentPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
            StatisticsQuery ctx, double absenceRateThreshold, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesPagedAsync(
            StatisticsQuery ctx, double absenceRateThreshold, PaginationQueryDto pagination,
            CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class StubAcademic : IAcademicStatisticsRepository
    {
        public Task<AcademicTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct) =>
            Task.FromResult(AcademicTotals);

        public Task<IReadOnlyList<AcademicGroup>> GetBySubjectAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(
            StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<AcademicGroup>> GetBySubjectPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<AcademicGroup>> GetByClassPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AcademicGroup>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<GradeDistributionResultDto> GetDistributionAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<AtRiskStudentDto>> GetAtRiskPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AtRiskClassCount>> GetAtRiskCountsByClassAsync(
            StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AcademicGroup>> GetSubjectGradeBaselinesAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class StubStudents : IStudentStatisticsRepository
    {
        public Task<StudentStatisticsDto> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct) =>
            Task.FromResult(StudentTotals);

        public Task<IReadOnlyList<CountRow>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<ClassCountRow>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<ClassCountRow>> GetByClassPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<CountRow>> GetByAgeAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<(DateOnly Month, int Count)>> GetEnrollmentTrendAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class StubTeachers : ITeacherStatisticsRepository
    {
        public Task<int> CountTeachersAsync(StatisticsQuery ctx, CancellationToken ct) =>
            Task.FromResult(25);

        public Task<IReadOnlyList<TeacherWorkloadDto>> GetWorkloadAsync(
            StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(Workload);

        public Task<IReadOnlyList<TeacherCoverageDto>> GetCoverageAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<PagedResultDto<TeacherWorkloadDto>> GetWorkloadPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<TeacherCoverageDto>> GetCoveragePagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<(Guid ClassId, string ClassName, int Grade, Guid SubjectId, string SubjectName,
            int GradedMarks, double? Average, int Passed)>> GetPerformanceAsync(
            StatisticsQuery ctx, Guid teacherId, CancellationToken ct) => throw new NotImplementedException();
        public Task<string?> GetTeacherNameAsync(StatisticsQuery ctx, Guid teacherId, CancellationToken ct) =>
            throw new NotImplementedException();
    }

    private sealed class StubEnrollments : IEnrollmentStatisticsRepository
    {
        public Task<int> CountActiveAsync(StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(480);
        public Task<int> CountStartedAsync(StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(35);
        public Task<int> CountExitedAsync(StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(12);
        public Task<int> CountActiveOnAsync(StatisticsQuery ctx, DateOnly on, CancellationToken ct) =>
            Task.FromResult(450);

        public Task<IReadOnlyList<CountRow>> GetExitsByReasonAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<CountRow>> GetByAcademicYearAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<ClassCapacityRow>> GetCapacityAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<PagedResultDto<ClassCapacityRow>> GetCapacityPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct) =>
            throw new NotImplementedException();
    }
}
