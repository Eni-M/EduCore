using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Rows;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface IStudentStatisticsRepository
{
    Task<StudentStatisticsDto> GetTotalsAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByGradeAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<ClassCountRow>> GetByClassAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<CountRow>> GetByAgeAsync(StatisticsQuery ctx, CancellationToken ct);

    // Active enrollments at the end of each month in the window.
    Task<IReadOnlyList<(DateOnly Month, int Count)>> GetEnrollmentTrendAsync(
        StatisticsQuery ctx, CancellationToken ct);
}
