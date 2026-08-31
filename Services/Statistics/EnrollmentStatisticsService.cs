using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services.Statistics;

public class EnrollmentStatisticsService(
    IEnrollmentStatisticsRepository enrollments,
    IStatisticsScopeResolver scope) : IEnrollmentStatisticsService
{
    public async Task<EnrollmentStatisticsDto> GetSummaryAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);

        var active = await enrollments.CountActiveAsync(ctx, ct);

        var yearAgo = await enrollments.CountActiveOnAsync(ctx, ctx.To.AddYears(-1), ct);

        return new EnrollmentStatisticsDto(
            active,
            await enrollments.CountStartedAsync(ctx, ct),
            await enrollments.CountExitedAsync(ctx, ct),
            Measures.GrowthPercent(active, yearAgo));
    }

    public async Task<IReadOnlyList<CountByKeyDto>> GetByAcademicYearAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        (await enrollments.GetByAcademicYearAsync(await scope.ResolveAsync(filter, ct), ct))
        .Select(r => new CountByKeyDto(r.Key, r.Label, r.Count))
        .ToList();

    public async Task<IReadOnlyList<CountByKeyDto>> GetExitsByReasonAsync(
        StatisticsFilter filter, CancellationToken ct) =>
        (await enrollments.GetExitsByReasonAsync(await scope.ResolveAsync(filter, ct), ct))
        .Select(r => new CountByKeyDto(r.Key, r.Label, r.Count))
        .ToList();

    public async Task<IReadOnlyList<ClassCapacityDto>> GetCapacityAsync(
        StatisticsFilter filter, CancellationToken ct)
    {
        var ctx = await scope.ResolveAsync(filter, ct);
        var rows = await enrollments.GetCapacityAsync(ctx, ct);

        return rows
            .Select(r => new ClassCapacityDto(
                r.ClassId,
                r.ClassName,
                r.Grade,
                r.Capacity,
                r.Enrolled,
                r.Capacity is null or 0
                    ? Measure.None("no capacity recorded")
                    : Measures.Rate(r.Enrolled, r.Capacity.Value)))
            .ToList();
    }
}
