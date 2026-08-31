namespace EduCore.Dtos.Statistics.Individual;

public record AtRiskStudentDto(
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    int Grade,
    double AverageFinalMark,
    int FailedSubjects,
    int AtRiskSubjects);
