using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics.Interfaces;

public interface IStatisticsPeriodResolver
{
    Task<(DateOnly From, DateOnly To)> ResolveAsync(
        StatisticsFilter filter, Guid? schoolId, CancellationToken ct);
}
