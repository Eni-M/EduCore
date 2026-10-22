namespace EduCore.Dtos.Mark;

public record MarkUpsertDto(
    Guid EnrollmentId,
    Guid SubjectId,
    Guid? GradingPeriodId,
    int? FirstMark,
    int? SecondMark,
    // Required to change a mark in a closed term; kept with the change either way.
    string? Reason = null,
    // The Version the client last saw (from MarkListDto). When sent, a mark changed by someone
    // else in between is refused with 409 rather than silently overwritten.
    uint? Version = null);
