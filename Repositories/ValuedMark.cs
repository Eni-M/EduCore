namespace EduCore.Repositories;

// A mark with its final value already worked out. Not an entity -- it is the shape the
// grading formula produces, so that formula can be written once and reused by anything that
// needs a final mark, statistics included.
public record ValuedMark(
    Guid MarkId,
    Guid EnrollmentId,
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    Guid GradingPeriodId,
    double Value);
