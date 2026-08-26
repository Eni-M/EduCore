namespace EduCore.Dtos.Statistics.Individual;

public record ExcessiveAbsenceStudentDto(
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    int LessonsRecorded,
    int Absences,
    int ExcusedAbsences,
    double AbsenceRate);
