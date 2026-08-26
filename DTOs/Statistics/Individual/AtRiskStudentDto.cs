namespace EduCore.Dtos.Statistics.Individual;

// Names a student, so this record can never be part of a public slice, and cell suppression
// cannot help it -- suppression protects aggregates, not lists of people.
public record AtRiskStudentDto(
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    int Grade,
    double AverageFinalMark,
    int FailedSubjects,
    int AtRiskSubjects);
