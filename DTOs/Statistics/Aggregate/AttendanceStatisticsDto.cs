namespace EduCore.Dtos.Statistics.Aggregate;

public record AttendanceStatisticsDto(
    int Records,
    Measure AttendanceRate,
    Measure AbsenceRate,
    Measure ExcusedAbsenceRate,
    Measure AverageBehaviorMark);
