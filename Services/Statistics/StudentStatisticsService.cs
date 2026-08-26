using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class StudentStatisticsService(
    IStudentStatisticsRepository students,
    IStatisticsScopeResolver scope,
    ICellSuppressor suppressor) : IStudentStatisticsService
{
    public async Task<StudentStatisticsDto> GetSummaryAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        await students.GetTotalsAsync(await scope.ResolveAsync(filter, ct), ct);

    public async Task<IReadOnlyList<CountByKeyDto>> GetByGradeAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        Map(await students.GetByGradeAsync(await scope.ResolveAsync(filter, ct), ct));

    public async Task<IReadOnlyList<CountByKeyDto>> GetByClassAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        Map(await students.GetByClassAsync(await scope.ResolveAsync(filter, ct), ct));

    public async Task<IReadOnlyList<CountByKeyDto>> GetByAgeAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        Map(await students.GetByAgeAsync(await scope.ResolveAsync(filter, ct), ct));

    public async Task<IReadOnlyList<TrendPointDto>> GetEnrollmentTrendAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var points = await students.GetEnrollmentTrendAsync(ctx, ct);

        double? previous = null;
        var trend = new List<TrendPointDto>();

        foreach (var (month, count) in points)
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);

            trend.Add(new TrendPointDto(
                month,
                monthEnd > ctx.To ? ctx.To : monthEnd,
                month.ToString("MMM yyyy"),
                suppressor.Apply(Measures.Count(count).WithPrevious(previous))));

            previous = count;
        }

        return trend;
    }

    // Counts are not Measures: a headcount is exact and has no baseline to sit against. They
    // still go through the same "Not recorded" handling that the repositories apply.
    private static IReadOnlyList<CountByKeyDto> Map(IEnumerable<Repositories.Statistics.CountRow> rows) =>
        rows.Select(r => new CountByKeyDto(r.Key, r.Label, r.Count)).ToList();
}
