using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IEnrollmentStatisticsRepository
{
    Task<int> CountActiveAsync(StatisticsContext ctx, CancellationToken ct);
    Task<int> CountStartedAsync(StatisticsContext ctx, CancellationToken ct);
    Task<int> CountExitedAsync(StatisticsContext ctx, CancellationToken ct);

    // Active enrollments as they stood one year before the window started, for growth.
    Task<int> CountActiveOnAsync(StatisticsContext ctx, DateOnly on, CancellationToken ct);

    Task<IReadOnlyList<CountRow>> GetExitsByReasonAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByAcademicYearAsync(StatisticsContext ctx, CancellationToken ct);

    Task<IReadOnlyList<(Guid ClassId, string ClassName, int Grade, int? Capacity, int Enrolled)>>
        GetCapacityAsync(StatisticsContext ctx, CancellationToken ct);
}
