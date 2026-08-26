using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics.Interfaces;

public interface IStatisticsScopeResolver
{
    Task<StatisticsContext> ResolveAsync(StatisticsFilter filter, CancellationToken ct);
}
