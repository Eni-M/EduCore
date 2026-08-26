namespace EduCore.Dtos.Statistics.Individual;

public record StudentAttendanceSummaryDto(
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    int LessonsRecorded,
    int Present,
    int Absent,
    int ExcusedAbsences,
    double AttendanceRate);
