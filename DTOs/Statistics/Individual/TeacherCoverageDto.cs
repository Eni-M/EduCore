namespace EduCore.Dtos.Statistics.Individual;

// Measures the teacher's own work rather than their students' outcomes, so nothing here is
// confounded by which class they were assigned.
//
// MissingAttendanceRecords counts lessons where fewer attendance rows exist than there were
// enrolled students -- i.e. the register was never completed.
public record TeacherCoverageDto(
    Guid TeacherId,
    string TeacherName,
    int LessonsLogged,
    int LessonsWithMissingAttendance,
    int MarksExpected,
    int MarksComplete,
    int MarksIncomplete,
    double MarkCompletionRate);
