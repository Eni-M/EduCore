namespace EduCore.Dtos.Statistics.Rows;

public record AttendanceTotals(
    int Records,
    int Present,
    int Absent,
    int ExcusedAbsences,
    double? AverageBehaviorMark,
    int BehaviorMarkCount);
