namespace EduCore.Dtos.Mark;

public record MarkUpsertDto(
    Guid EnrollmentId,
    Guid SubjectId,
    Guid? GradingPeriodId,
    int? FirstMark,
    int? SecondMark);
