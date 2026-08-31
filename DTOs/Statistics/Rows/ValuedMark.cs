namespace EduCore.Dtos.Statistics.Rows;

public record ValuedMark(
    Guid MarkId,
    Guid EnrollmentId,
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    Guid GradingPeriodId,
    double Value);
