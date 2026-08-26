using EduCore.Dtos.Statistics.Aggregate;

namespace EduCore.Dtos.Statistics.Individual;

// One class-subject the teacher teaches. AverageFinalMark always carries the school average
// for the same subject and grade as its Baseline, and the previous grading period's figure as
// Previous -- a bare number here would read as a verdict on the teacher.
public record TeacherSubjectPerformanceDto(
    Guid ClassId,
    string ClassName,
    int Grade,
    Guid SubjectId,
    string SubjectName,
    int GradedMarks,
    Measure AverageFinalMark,
    Measure PassRate);
