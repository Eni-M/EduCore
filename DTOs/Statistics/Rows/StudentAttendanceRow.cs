namespace EduCore.Dtos.Statistics.Rows;

public record StudentAttendanceRow(
    Guid StudentId,
    string Name,
    Guid ClassId,
    string ClassName,
    int Records,
    int Present,
    int Excused);
