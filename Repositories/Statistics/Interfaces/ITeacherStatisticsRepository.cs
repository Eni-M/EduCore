using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Services.Statistics;

namespace EduCore.Repositories.Statistics.Interfaces;

public interface ITeacherStatisticsRepository
{
    Task<int> CountTeachersAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<TeacherWorkloadDto>> GetWorkloadAsync(StatisticsQuery ctx, CancellationToken ct);
    Task<IReadOnlyList<TeacherCoverageDto>> GetCoverageAsync(StatisticsQuery ctx, CancellationToken ct);

    Task<IReadOnlyList<(Guid ClassId, string ClassName, int Grade, Guid SubjectId, string SubjectName,
        int GradedMarks, double? Average, int Passed)>> GetPerformanceAsync(
        StatisticsQuery ctx, Guid teacherId, CancellationToken ct);

    Task<string?> GetTeacherNameAsync(StatisticsQuery ctx, Guid teacherId, CancellationToken ct);
}
