using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;

namespace EduCore.Services.Statistics.Interfaces;

// The statistics surface, grouped by the six areas plus demographics and the two dashboard
// compositions. Every method takes a StatisticsFilter and resolves it through
// StatisticsScopeResolver before touching the database.

public interface IStudentStatisticsService
{
    Task<StudentStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetByGradeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetByClassAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetByAgeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<TrendPointDto>> GetEnrollmentTrendAsync(StatisticsFilter filter, CancellationToken ct);
}

public interface IEnrollmentStatisticsService
{
    Task<EnrollmentStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetByAcademicYearAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<CountByKeyDto>> GetExitsByReasonAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<ClassCapacityDto>> GetCapacityAsync(StatisticsFilter filter, CancellationToken ct);
}

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

public interface IAcademicStatisticsService
{
    Task<AcademicStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AcademicByKeyDto>> GetBySubjectAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AcademicByKeyDto>> GetByClassAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AcademicByKeyDto>> GetByGradeAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<GradeDistributionDto>> GetDistributionAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AtRiskStudentDto>> GetAtRiskAsync(StatisticsFilter filter, CancellationToken ct);
    Task<SubjectComparisonDto> GetSubjectComparisonAsync(
        StatisticsFilter filter, Guid subjectId, CancellationToken ct);
}

public interface ITeacherStatisticsService
{
    Task<TeacherStatisticsDto> GetSummaryAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<TeacherWorkloadDto>> GetWorkloadAsync(StatisticsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<TeacherCoverageDto>> GetCoverageAsync(StatisticsFilter filter, CancellationToken ct);
    Task<TeacherDetailStatisticsDto> GetDetailAsync(
        StatisticsFilter filter, Guid teacherId, CancellationToken ct);
}

public interface IClassStatisticsService
{
    Task<IReadOnlyList<ClassSummaryDto>> GetSummariesAsync(StatisticsFilter filter, CancellationToken ct);
    Task<ClassDetailStatisticsDto> GetDetailAsync(StatisticsFilter filter, Guid classId, CancellationToken ct);
}

public interface IDemographicsStatisticsService
{
    Task<DemographicsOverviewDto> GetOverviewAsync(CancellationToken ct);
    Task<IReadOnlyList<ZoneDemographicsDto>> GetByZoneAsync(string? city, CancellationToken ct);
}

public interface IStatisticsOverviewService
{
    Task<SchoolOverviewDto> GetSchoolOverviewAsync(StatisticsFilter filter, CancellationToken ct);
    Task<PlatformOverviewDto> GetPlatformOverviewAsync(StatisticsFilter filter, CancellationToken ct);
}
