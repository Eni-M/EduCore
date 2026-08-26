namespace EduCore.Dtos.Statistics.Aggregate;

// Rates are percentages of recorded attendance rows in the window. ExcusedAbsenceRate is a
// share of all records, not of absences, so the three rates sum to 100.
public record AttendanceStatisticsDto(
    int Records,
    Measure AttendanceRate,
    Measure AbsenceRate,
    Measure ExcusedAbsenceRate,
    Measure AverageBehaviorMark);
