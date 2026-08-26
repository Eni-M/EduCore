using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IStudentStatisticsRepository
{
    Task<StudentStatisticsDto> GetTotalsAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByGradeAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByClassAsync(StatisticsContext ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByAgeAsync(StatisticsContext ctx, CancellationToken ct);

    // Active enrollments at the end of each month in the window.
    Task<IReadOnlyList<(DateOnly Month, int Count)>> GetEnrollmentTrendAsync(
        StatisticsContext ctx, CancellationToken ct);
}
