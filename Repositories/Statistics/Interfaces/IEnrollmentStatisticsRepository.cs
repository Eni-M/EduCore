using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IEnrollmentStatisticsRepository
{
    Task<int> CountActiveAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<int> CountStartedAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<int> CountExitedAsync(StatisticsQuery ctx, CancellationToken ct);

    // Active enrollments as they stood one year before the window started, for growth.
    Task<int> CountActiveOnAsync(StatisticsQuery ctx, DateOnly on, CancellationToken ct);

    Task<IReadOnlyList<CountRow>> GetExitsByReasonAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByAcademicYearAsync(StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<(Guid ClassId, string ClassName, int Grade, int? Capacity, int Enrolled)>>
        GetCapacityAsync(StatisticsQuery ctx, CancellationToken ct);
}
