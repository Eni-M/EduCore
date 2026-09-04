using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Services.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Xunit;

namespace EduCore.Tests;

// Guards the P0 perf work: the paged class-summary path must compose exactly the same
// summaries as the full-list path, skip per-class aggregates for empty pages, and use
// the same page clamping as the in-memory pager.
public sealed class ClassStatisticsServiceTests
{
    private static readonly StatisticsQuery Ctx = new(
        null, true, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
        null, null, null, null, null, null, null, null, null, null, null);

    private static readonly Guid ClassA = Guid.NewGuid();
    private static readonly Guid ClassB = Guid.NewGuid();
    private static readonly Guid ClassC = Guid.NewGuid();

    private static readonly IReadOnlyList<ClassCountRow> Classes =
    [
        new(ClassA, "Alpha", 5, 30),
        new(ClassB, "Beta", 5, 25),
        new(ClassC, "Gamma", 6, 20),
    ];

    private static readonly IReadOnlyList<AcademicGroup> AcademicGroups =
    [
        new(ClassA.ToString(), "Alpha", 100, 4.0, 95),
        new(ClassB.ToString(), "Beta", 80, 3.0, 60),
    ];

    private static readonly IReadOnlyList<AttendanceGroup> AttendanceGroups =
    [
        new(ClassA.ToString(), "Alpha", 200, 190, 5),
        new(ClassC.ToString(), "Gamma", 150, 120, 10),
    ];

    private static readonly IReadOnlyList<AtRiskStudentDto> AtRiskStudents =
    [
        new(Guid.NewGuid(), "Student One", ClassA, "Alpha", 5, 2.0, 1, 1),
        new(Guid.NewGuid(), "Student Two", ClassA, "Alpha", 5, 1.8, 2, 0),
        new(Guid.NewGuid(), "Student Three", ClassB, "Beta", 5, 2.2, 0, 2),
    ];

    private static readonly IReadOnlyList<AtRiskClassCount> AtRiskCounts =
    [
        new(ClassA, 2),
        new(ClassB, 1),
    ];

    private static readonly AcademicTotals AcademicTotals = new(180, 5, 3.5, 155, 25, 10);
    private static readonly AttendanceTotals AttendanceTotals = new(100, 80, 20, 8, null, 0);

    private sealed class StubScope : IStatisticsScopeResolver
    {
        public Task<StatisticsQuery> ResolveAsync(StatisticsFilter filter, CancellationToken ct) =>
            Task.FromResult(Ctx);
    }

    private sealed class PassThroughSuppressor : ICellSuppressor
    {
        public Measure Apply(Measure measure) => measure;
        public IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures) => measures.ToList();
        public bool ShouldSuppress(int sampleSize) => false;
    }

    private sealed class FakeStudents(
        IReadOnlyList<ClassCountRow> full,
        PagedResultDto<ClassCountRow> page) : Repositories.Statistics.Interfaces.IStudentStatisticsRepository
    {
        public PaginationQueryDto? ReceivedPagination { get; private set; }

        public Task<IReadOnlyList<ClassCountRow>> GetByClassAsync(
            StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(full);

        public Task<PagedResultDto<ClassCountRow>> GetByClassPagedAsync(
            StatisticsQuery ctx, PaginationQueryDto pagination, CancellationToken ct)
        {
            ReceivedPagination = pagination;
            return Task.FromResult(page);
        }

        public Task<StudentStatisticsDto> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<CountRow>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<CountRow>> GetByAgeAsync(StatisticsQuery ctx, CancellationToken ct) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<(DateOnly Month, int Count)>> GetEnrollmentTrendAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeAcademic : Repositories.Statistics.Interfaces.IAcademicStatisticsRepository
    {
        public int RestrictedByClassCalls;
        public int AtRiskCountCalls;
        public int TotalsCalls;
        public IReadOnlyCollection<Guid>? ReceivedIds;

        public Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(
            StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(AcademicGroups);

        public Task<IReadOnlyList<AcademicGroup>> GetByClassAsync(
            StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct)
        {
            RestrictedByClassCalls++;
            ReceivedIds = classIds;
            return Task.FromResult(AcademicGroups);
        }

        public Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(
            StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(AtRiskStudents);

        public Task<IReadOnlyList<AtRiskClassCount>> GetAtRiskCountsByClassAsync(
            StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct)
        {
            AtRiskCountCalls++;
            ReceivedIds = classIds;
            return Task.FromResult(AtRiskCounts);
        }

        public Task<AcademicTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct)
        {
            TotalsCalls++;
            return Task.FromResult(AcademicTotals);
        }

        public Task<IReadOnlyList<AcademicGroup>> GetBySubjectAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<AcademicGroup>> GetByGradeAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<GradeDistributionResultDto> GetDistributionAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<AcademicGroup>> GetSubjectGradeBaselinesAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeAttendance : Repositories.Statistics.Interfaces.IAttendanceStatisticsRepository
    {
        public int RestrictedByClassCalls;
        public int TotalsCalls;
        public IReadOnlyCollection<Guid>? ReceivedIds;

        public Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(
            StatisticsQuery ctx, CancellationToken ct) => Task.FromResult(AttendanceGroups);

        public Task<IReadOnlyList<AttendanceGroup>> GetByClassAsync(
            StatisticsQuery ctx, IReadOnlyCollection<Guid> classIds, CancellationToken ct)
        {
            RestrictedByClassCalls++;
            ReceivedIds = classIds;
            return Task.FromResult(AttendanceGroups);
        }

        public Task<AttendanceTotals> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct)
        {
            TotalsCalls++;
            return Task.FromResult(AttendanceTotals);
        }

        public Task<IReadOnlyList<AttendanceGroup>> GetByGradeAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<AttendanceGroup>> GetBySubjectAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<(DateOnly Day, int Records, int Present)>> GetDailyTrendAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(
            StatisticsQuery ctx, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
            StatisticsQuery ctx, double absenceRateThreshold, CancellationToken ct) =>
            throw new NotImplementedException();
    }

    private static ClassStatisticsService Service(
        FakeStudents students, FakeAcademic academic, FakeAttendance attendance) =>
        new(academic, attendance, students, new StubScope(), new PassThroughSuppressor());

    [Fact]
    public async Task Paged_ReturnsSameSummariesAsFullList()
    {
        var students = new FakeStudents(Classes, new PagedResultDto<ClassCountRow>(Classes, 1, 20, 3));
        var academic = new FakeAcademic();
        var attendance = new FakeAttendance();
        var service = Service(students, academic, attendance);

        var full = await service.GetSummariesAsync(new StatisticsFilter(), CancellationToken.None);
        var paged = await service.GetSummariesPagedAsync(
            new StatisticsFilter(), new PaginationQueryDto(1, 20), CancellationToken.None);

        Assert.Equal(full, paged.Items.ToList());
        Assert.Equal(3, paged.TotalCount);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
    }

    [Fact]
    public async Task Paged_ForwardsPageClassIdsToRestrictedAggregates()
    {
        var students = new FakeStudents(Classes, new PagedResultDto<ClassCountRow>(Classes, 1, 20, 3));
        var academic = new FakeAcademic();
        var attendance = new FakeAttendance();
        var service = Service(students, academic, attendance);

        await service.GetSummariesPagedAsync(
            new StatisticsFilter(), new PaginationQueryDto(1, 20), CancellationToken.None);

        Assert.Equal([ClassA, ClassB, ClassC], academic.ReceivedIds);
        Assert.Equal([ClassA, ClassB, ClassC], attendance.ReceivedIds);
        Assert.Equal(new PaginationQueryDto(1, 20), students.ReceivedPagination);
    }

    [Fact]
    public async Task EmptyPage_SkipsPerClassAggregatesButKeepsTotals()
    {
        var students = new FakeStudents(
            Classes, new PagedResultDto<ClassCountRow>([], 99, 20, 3));
        var academic = new FakeAcademic();
        var attendance = new FakeAttendance();
        var service = Service(students, academic, attendance);

        var paged = await service.GetSummariesPagedAsync(
            new StatisticsFilter(), new PaginationQueryDto(99, 20), CancellationToken.None);

        Assert.Empty(paged.Items);
        Assert.Equal(3, paged.TotalCount);
        Assert.Equal(0, academic.RestrictedByClassCalls);
        Assert.Equal(0, academic.AtRiskCountCalls);
        Assert.Equal(0, attendance.RestrictedByClassCalls);
        // Scope-level baselines still run so headline figures stay full-scope.
        Assert.Equal(1, academic.TotalsCalls);
        Assert.Equal(1, attendance.TotalsCalls);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, -5, 1, 1)]
    [InlineData(2, 500, 2, 100)]
    [InlineData(1, 20, 1, 20)]
    public void Normalize_ClampsPageAndPageSize(int page, int pageSize, int expPage, int expSize)
    {
        Assert.Equal((expPage, expSize), ListPagination.Normalize(new PaginationQueryDto(page, pageSize)));
    }
}
