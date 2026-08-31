namespace EduCore.Dtos.Statistics.Individual;

public record TeacherDetailStatisticsDto(
    TeacherWorkloadDto Workload,
    TeacherCoverageDto Coverage,
    IReadOnlyList<TeacherSubjectPerformanceDto> Performance);
