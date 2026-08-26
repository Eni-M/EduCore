using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IClassStatisticsService
{
    Task<IReadOnlyList<ClassSummaryDto>> GetSummariesAsync(StatisticsFilter filter, CancellationToken ct);
    Task<ClassDetailStatisticsDto> GetDetailAsync(StatisticsFilter filter, Guid classId, CancellationToken ct);
}
