namespace EduCore.Dtos.Statistics.Rows;

public record AttendanceTotals(
    int Records,
    int Present,
    int Absent,
    int ExcusedAbsences,
    double? AverageBehaviorMark,
    // Behaviour marks are optional, so far fewer rows carry one than exist in Records. Using
    // anything else as the average's sample size would let one child's mark past suppression.
    int BehaviorMarkCount);
