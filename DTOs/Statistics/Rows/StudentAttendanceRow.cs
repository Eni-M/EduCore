namespace EduCore.Dtos.Statistics.Rows;

// One student's attendance tallies for a window, before any rate is worked out.
public record StudentAttendanceRow(
    Guid StudentId,
    string Name,
    Guid ClassId,
    string ClassName,
    int Records,
    int Present,
    int Excused);
