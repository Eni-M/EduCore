using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics.Interfaces;

public interface IStatisticsScopeResolver
{
    Task<StatisticsQuery> ResolveAsync(StatisticsFilter filter, CancellationToken ct);
}
