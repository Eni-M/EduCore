using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IDemographicsStatisticsService
{
    Task<DemographicsOverviewDto> GetOverviewAsync(CancellationToken ct);
    Task<IReadOnlyList<ZoneDemographicsDto>> GetByZoneAsync(string? city, CancellationToken ct);
}
