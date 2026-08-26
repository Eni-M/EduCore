namespace EduCore.Dtos.Statistics.Rows;

// A mark with its final value worked out. Produced by MarkRepository.Value so the grading
// formula is written once.
//
// Only usable for reading rows, never for aggregating: EF cannot see through a constructed
// type, so averaging, joining or grouping on one of these fails to translate. Those paths use
// MarkRepository.WeightedValues instead.
public record ValuedMark(
    Guid MarkId,
    Guid EnrollmentId,
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    Guid GradingPeriodId,
    double Value);
