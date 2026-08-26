namespace EduCore.Repositories.Statistics;

// Raw shapes the statistics repositories return: plain numbers straight out of SQL, with no
// baselines, no suppression and no formatting. Turning these into Measures is the services'
// job, which keeps the queries readable and the presentation rules in one place.
public record AcademicTotals(
    int GradedMarks,
    int IncompleteMarks,
    double? AverageFinalMark,
    int Passed,
    int Failed,
    int AtRisk);

public record AcademicGroup(
    string Key,
    string Label,
    int GradedMarks,
    double? AverageFinalMark,
    int Passed);

public record AttendanceTotals(
    int Records,
    int Present,
    int Absent,
    int ExcusedAbsences,
    double? AverageBehaviorMark);

public record AttendanceGroup(
    string Key,
    string Label,
    int Records,
    int Present,
    int ExcusedAbsences);

public record CountRow(string Key, string Label, int Count);
