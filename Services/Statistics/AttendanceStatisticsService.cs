using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Repositories.Statistics;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class AttendanceStatisticsService(
    IAttendanceStatisticsRepository attendance,
    IStatisticsScopeResolver scope,
    ICellSuppressor suppressor) : IAttendanceStatisticsService
{
    private const double DefaultExcessiveAbsenceRate = 20.0;

    public async Task<AttendanceStatisticsDto> GetSummaryAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var totals = await attendance.GetTotalsAsync(ctx, ct);

        return new AttendanceStatisticsDto(
            totals.Records,
            suppressor.Apply(Measures.Rate(totals.Present, totals.Records)),
            suppressor.Apply(Measures.Rate(totals.Absent, totals.Records)),
            suppressor.Apply(Measures.Rate(totals.ExcusedAbsences, totals.Records)),
            suppressor.Apply(Measures.Average(totals.AverageBehaviorMark, totals.BehaviorMarkCount)));
    }

    public async Task<IReadOnlyList<AttendanceByKeyDto>> GetByClassAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return WithBaseline(
            await attendance.GetByClassAsync(ctx, ct),
            await attendance.GetTotalsAsync(ctx, ct),
            "school attendance rate");
    }

    public async Task<PagedResultDto<AttendanceByKeyDto>> GetByClassPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var page = await attendance.GetByClassPagedAsync(ctx, pagination, ct);

        // Baseline over the whole scope, not the page: the school rate a class is compared
        // against must not shift as the reader turns the page.
        return Repage(page, WithBaseline(
            page.Items, await attendance.GetTotalsAsync(ctx, ct), "school attendance rate"));
    }

    public async Task<PagedResultDto<AttendanceByKeyDto>> GetBySubjectPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var page = await attendance.GetBySubjectPagedAsync(ctx, pagination, ct);

        return Repage(page, WithBaseline(
            page.Items, await attendance.GetTotalsAsync(ctx, ct), "school attendance rate"));
    }

    private static PagedResultDto<AttendanceByKeyDto> Repage(
        PagedResultDto<AttendanceGroup> source, IReadOnlyList<AttendanceByKeyDto> items) =>
        new(items, source.Page, source.PageSize, source.TotalCount);

    public async Task<IReadOnlyList<AttendanceByKeyDto>> GetByGradeAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return WithBaseline(
            await attendance.GetByGradeAsync(ctx, ct),
            await attendance.GetTotalsAsync(ctx, ct),
            "school attendance rate");
    }

    public async Task<IReadOnlyList<AttendanceByKeyDto>> GetBySubjectAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        return WithBaseline(
            await attendance.GetBySubjectAsync(ctx, ct),
            await attendance.GetTotalsAsync(ctx, ct),
            "school attendance rate");
    }

    public async Task<IReadOnlyList<TrendPointDto>> GetTrendAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var days = await attendance.GetDailyTrendAsync(ctx, ct);

        double? previous = null;
        var trend = new List<TrendPointDto>();

        foreach (var (day, records, present) in days)
        {
            var rate = records == 0 ? (double?)null : Math.Round(present * 100.0 / records, 2);

            trend.Add(new TrendPointDto(
                day,
                day,
                day.ToString("dd MMM yyyy"),
                suppressor.Apply(Measures.Rate(present, records).WithPrevious(previous))));

            previous = rate;
        }

        return trend;
    }

    public async Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        await attendance.GetByStudentAsync(await scope.ResolveAsync(filter, ct), ct);

    public async Task<PagedResultDto<StudentAttendanceSummaryDto>> GetByStudentPagedAsync(
        StatisticsFilter filter, PaginationQueryDto pagination, CancellationToken ct) =>
        await attendance.GetByStudentPagedAsync(await scope.ResolveAsync(filter, ct), pagination, ct);

    public async Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsFilter filter, double? thresholdPercent, CancellationToken ct) =>
        await attendance.GetExcessiveAbsencesAsync(
            await scope.ResolveAsync(filter, ct),
            thresholdPercent ?? DefaultExcessiveAbsenceRate,
            ct);

    public async Task<PagedResultDto<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesPagedAsync(
        StatisticsFilter filter, double? thresholdPercent, PaginationQueryDto pagination, CancellationToken ct) =>
        await attendance.GetExcessiveAbsencesPagedAsync(
            await scope.ResolveAsync(filter, ct),
            thresholdPercent ?? DefaultExcessiveAbsenceRate,
            pagination,
            ct);

    private IReadOnlyList<AttendanceByKeyDto> WithBaseline(
        IEnumerable<AttendanceGroup> groups, AttendanceTotals overall, string baselineLabel)
    {
        var baseline = overall.Records == 0
            ? (double?)null
            : Math.Round(overall.Present * 100.0 / overall.Records, 2);

        return groups
            .Select(g => new AttendanceByKeyDto(
                g.Key,
                g.Label,
                g.Records,
                suppressor.Apply(Measures.Rate(g.Present, g.Records).WithBaseline(baseline, baselineLabel)),
                suppressor.Apply(Measures.Rate(g.ExcusedAbsences, g.Records))))
            .ToList();
    }
}
