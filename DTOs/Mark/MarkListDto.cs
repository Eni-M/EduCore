namespace EduCore.Dtos.Mark;

// FinalMark is computed from the two marks, never stored -- it is null until both are set,
// which is how a half-graded period reads as incomplete rather than as a low grade.
public record MarkListDto(
    Guid Id,
    Guid EnrollmentId,
    string StudentName,
    string ClassName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid GradingPeriodId,
    string GradingPeriodName,
    int? FirstMark,
    int? SecondMark,
    int? FinalMark);
