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
    double? AverageBehaviorMark,
    // Behaviour marks are optional, so far fewer rows carry one than exist in Records. This is
    // the count the average was actually taken over, and using anything else as its sample
    // size would let a single child's behaviour mark pass through suppression untouched.
    int BehaviorMarkCount);

public record AttendanceGroup(
    string Key,
    string Label,
    int Records,
    int Present,
    int ExcusedAbsences);

public record CountRow(string Key, string Label, int Count);

// A per-class count. Separate from CountRow because a class breakdown needs the real id and
// grade -- stringifying the id and parsing it back on the other side loses both.
public record ClassCountRow(Guid ClassId, string ClassName, int Grade, int Count);
