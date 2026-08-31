using EduCore.Dtos.Statistics.Aggregate;

namespace EduCore.Dtos.Statistics.Individual;

public record TeacherSubjectPerformanceDto(
    Guid ClassId,
    string ClassName,
    int Grade,
    Guid SubjectId,
    string SubjectName,
    int GradedMarks,
    Measure AverageFinalMark,
    Measure PassRate);
