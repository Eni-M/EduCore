using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

public interface IAttendanceStatisticsService
{
    Task<AttendanceStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AttendanceByKeyDto>> GetByClassAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AttendanceByKeyDto>> GetByGradeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AttendanceByKeyDto>> GetBySubjectAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<TrendPointDto>> GetTrendAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetByStudentAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<ExcessiveAbsenceStudentDto>> GetExcessiveAbsencesAsync(
        StatisticsFilter filter, double? thresholdPercent, CancellationToken ct);
}
