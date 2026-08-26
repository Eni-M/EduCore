namespace EduCore.Dtos.Statistics.Aggregate;

public record AttendanceByKeyDto(
    string Key,
    string Label,
    int Records,
    Measure AttendanceRate,
    Measure ExcusedAbsenceRate);
