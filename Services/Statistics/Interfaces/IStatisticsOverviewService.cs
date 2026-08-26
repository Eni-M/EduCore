using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IStatisticsOverviewService
{
    Task<SchoolOverviewDto> GetSchoolOverviewAsync(StatisticsFilter filter, CancellationToken ct);
    Task<PlatformOverviewDto> GetPlatformOverviewAsync(StatisticsFilter filter, CancellationToken ct);
}
