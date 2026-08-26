namespace EduCore.Dtos.Statistics.Aggregate;

public record ClassDetailStatisticsDto(
    ClassSummaryDto Summary,
    IReadOnlyList<AcademicByKeyDto> Subjects,
    IReadOnlyList<AttendanceByKeyDto> AttendanceBySubject,
    IReadOnlyList<GradeDistributionDto> GradeDistribution);
