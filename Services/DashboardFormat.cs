using EduCore.Dtos.Dashboard;
using EduCore.Dtos.Statistics;

namespace EduCore.Services;

/// <summary>
/// The judgement calls behind a dashboard: how a measure is rendered, and when a figure is good,
/// worth watching, or worth acting on.
///
/// Pure and separate from <see cref="DashboardService"/> on purpose, following the same split as
/// CellSuppressor, SnapshotStaleness and GradingCalculator. These are the rules most worth testing
/// exhaustively — one of them decides whether a privacy-suppressed statistic reaches a screen —
/// and keeping them out of the service means testing them needs no fakes for six other services.
/// </summary>
public static class DashboardFormat
{
    /// <summary>
    /// A measure as a percentage, or an em dash when there is nothing to show.
    ///
    /// Suppression is checked before the value, and that order is the point. A measure the privacy
    /// layer withheld for covering too few students still carries its value; a formatter that only
    /// asked "is there a number?" would put it back on screen and undo the suppression.
    /// </summary>
    public static string Percent(Measure measure) =>
        Hidden(measure) ? "—" : $"{Round(measure.Value!.Value)}%";

    /// <summary>A rate where a higher number is better — attendance, pass rate.</summary>
    public static DashboardTone RateTone(Measure measure, double good, double warn) =>
        Hidden(measure) ? DashboardTone.Neutral
        : measure.Value >= good ? DashboardTone.Good
        : measure.Value >= warn ? DashboardTone.Warning
        : DashboardTone.Critical;

    /// <summary>A rate where a higher number is worse — students at risk, failure rate.</summary>
    public static DashboardTone InverseRateTone(Measure measure, double good, double warn) =>
        Hidden(measure) ? DashboardTone.Neutral
        : measure.Value <= good ? DashboardTone.Good
        : measure.Value <= warn ? DashboardTone.Warning
        : DashboardTone.Critical;

    /// <summary>
    /// Attendance over a set of records. Neutral rather than Critical when nothing is recorded:
    /// a class whose register has not been taken yet is not a class with terrible attendance.
    /// </summary>
    public static DashboardTone AttendanceTone(int present, int total) =>
        total == 0 ? DashboardTone.Neutral
        : present * 100.0 / total >= 95 ? DashboardTone.Good
        : present * 100.0 / total >= 90 ? DashboardTone.Warning
        : DashboardTone.Critical;

    /// <summary>
    /// An attendance percentage, or an em dash when nothing has been recorded. Zero would read as
    /// "never attended" rather than "no register taken".
    /// </summary>
    public static string AttendancePercent(int present, int total) =>
        total == 0 ? "—" : $"{Round(present * 100.0 / total)}%";

    /// <summary>
    /// An average of already-final marks, or an em dash when there are none. Never zero: on a
    /// 1-to-5 scale a zero is not a neutral placeholder, it is worse than the worst grade.
    /// </summary>
    public static string Average(IReadOnlyCollection<int> finalMarks) =>
        finalMarks.Count == 0
            ? "—"
            : Math.Round(finalMarks.Average(), 2, MidpointRounding.AwayFromZero).ToString("0.##");

    /// <summary>
    /// Whether a figure should be treated as absent — either withheld by the privacy layer, or
    /// never computed at all. The two are different upstream and identical to a reader.
    /// </summary>
    private static bool Hidden(Measure measure) => measure.Suppressed || measure.Value is null;

    // AwayFromZero, matching GradingCalculator. .NET rounds midpoints to even by default, so a
    // pass rate of exactly 96.5% would render as 96% here and 97% anywhere the grading rules did
    // the rounding. One convention, applied everywhere marks and rates are presented.
    private static double Round(double value) => Math.Round(value, MidpointRounding.AwayFromZero);
}
