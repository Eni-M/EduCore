using EduCore.Dtos.Dashboard;
using EduCore.Dtos.Statistics;

namespace EduCore.Services;

// How a dashboard measure is rendered, and when a figure is good, worth watching, or worth
// acting on. Pure, so it tests without fakes for the services DashboardService composes.
public static class DashboardFormat
{
    // Suppression is checked before the value: a withheld measure still carries one, and asking
    // only "is there a number?" would put it back on screen.
    public static string Percent(Measure measure) =>
        Hidden(measure) ? "—" : $"{Round(measure.Value!.Value)}%";

    // Higher is better — attendance, pass rate.
    public static DashboardTone RateTone(Measure measure, double good, double warn) =>
        Hidden(measure) ? DashboardTone.Neutral
        : measure.Value >= good ? DashboardTone.Good
        : measure.Value >= warn ? DashboardTone.Warning
        : DashboardTone.Critical;

    // Higher is worse — students at risk, failure rate.
    public static DashboardTone InverseRateTone(Measure measure, double good, double warn) =>
        Hidden(measure) ? DashboardTone.Neutral
        : measure.Value <= good ? DashboardTone.Good
        : measure.Value <= warn ? DashboardTone.Warning
        : DashboardTone.Critical;

    // Neutral rather than Critical on an empty register: not taken is not terrible attendance.
    public static DashboardTone AttendanceTone(int present, int total) =>
        total == 0 ? DashboardTone.Neutral
        : present * 100.0 / total >= 95 ? DashboardTone.Good
        : present * 100.0 / total >= 90 ? DashboardTone.Warning
        : DashboardTone.Critical;

    public static string AttendancePercent(int present, int total) =>
        total == 0 ? "—" : $"{Round(present * 100.0 / total)}%";

    // Never zero: on a 1-to-5 scale zero is worse than the worst grade, not a placeholder.
    public static string Average(IReadOnlyCollection<int> finalMarks) =>
        finalMarks.Count == 0
            ? "—"
            : Math.Round(finalMarks.Average(), 2, MidpointRounding.AwayFromZero).ToString("0.##");

    // Withheld by the privacy layer or never computed: different upstream, identical to a reader.
    private static bool Hidden(Measure measure) => measure.Suppressed || measure.Value is null;

    // AwayFromZero, matching GradingCalculator, so 96.5% does not render as 96% here and 97% there.
    private static double Round(double value) => Math.Round(value, MidpointRounding.AwayFromZero);
}
