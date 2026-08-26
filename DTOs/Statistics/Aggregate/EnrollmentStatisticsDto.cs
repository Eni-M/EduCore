namespace EduCore.Dtos.Statistics.Aggregate;

public record EnrollmentStatisticsDto(
    int ActiveEnrollments,
    int NewEnrollments,
    int Exits,
    Measure GrowthPercent);
