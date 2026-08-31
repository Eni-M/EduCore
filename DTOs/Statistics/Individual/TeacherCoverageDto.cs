namespace EduCore.Dtos.Statistics.Individual;

public record TeacherCoverageDto(
    Guid TeacherId,
    string TeacherName,
    Guid SchoolId,
    string SchoolName,
    int LessonsLogged,
    int LessonsWithMissingAttendance,
    int MarksExpected,
    int MarksComplete,
    int MarksIncomplete,
    double MarkCompletionRate);
