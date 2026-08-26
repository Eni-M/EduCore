namespace EduCore.Dtos.Statistics.Rows;

public record AttendanceGroup(
    string Key,
    string Label,
    int Records,
    int Present,
    int ExcusedAbsences);
