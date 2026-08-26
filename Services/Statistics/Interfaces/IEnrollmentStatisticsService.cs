using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IEnrollmentStatisticsService
{
    Task<EnrollmentStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetByAcademicYearAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetExitsByReasonAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<ClassCapacityDto>> GetCapacityAsync(StatisticsFilter filter, CancellationToken ct);
}
