using EduCore.Dtos.Dashboard;
using EduCore.Dtos.Statistics;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

// The judgement calls a dashboard makes: what a figure is rendered as, and whether a user is
// told to act on it. One of these decides whether a privacy-suppressed statistic reaches a
// screen, which makes it the most security-relevant formatting code in the application.
public class DashboardFormatTests
{
    private static Measure Rate(double value) => Measure.Of(value, 200);

    // Carries a real value that the privacy layer withheld because too few students are behind it.
    private static Measure Withheld(double value) =>
        new(value, null, null, null, SampleSize: 3, Suppressed: true);

    // ---- suppression ---------------------------------------------------------------

    [Fact]
    public void A_withheld_measure_never_renders_its_value()
    {
        // The reason Percent checks Suppressed before Value. Asking only "is there a number?"
        // would put the withheld figure straight back on screen and undo the suppression that
        // CellSuppressor applied upstream.
        Assert.Equal("—", DashboardFormat.Percent(Withheld(87.5)));
    }

    [Fact]
    public void A_withheld_measure_is_never_judged_good_or_bad()
    {
        // Tone leaks too, if more slowly. Colouring a hidden figure red tells the reader roughly
        // what it was, which is most of what the suppression was protecting.
        Assert.Equal(DashboardTone.Neutral, DashboardFormat.RateTone(Withheld(12), 95, 90));
        Assert.Equal(DashboardTone.Neutral, DashboardFormat.InverseRateTone(Withheld(80), 5, 15));
    }

    [Fact]
    public void A_measure_with_no_value_renders_as_absent()
    {
        Assert.Equal("—", DashboardFormat.Percent(Measure.None()));
        Assert.Equal(DashboardTone.Neutral, DashboardFormat.RateTone(Measure.None(), 95, 90));
    }

    // ---- percentages ---------------------------------------------------------------

    [Theory]
    [InlineData(96.4, "96%")]
    [InlineData(96.5, "97%")]
    [InlineData(0, "0%")]
    [InlineData(100, "100%")]
    public void A_present_measure_renders_as_a_whole_percentage(double value, string expected)
    {
        Assert.Equal(expected, DashboardFormat.Percent(Rate(value)));
    }

    // ---- tone thresholds -------------------------------------------------------------

    [Theory]
    [InlineData(99, DashboardTone.Good)]
    [InlineData(95, DashboardTone.Good)]
    [InlineData(94.9, DashboardTone.Warning)]
    [InlineData(90, DashboardTone.Warning)]
    [InlineData(89.9, DashboardTone.Critical)]
    [InlineData(0, DashboardTone.Critical)]
    public void A_higher_is_better_rate_is_judged_against_both_thresholds(double value, DashboardTone expected)
    {
        Assert.Equal(expected, DashboardFormat.RateTone(Rate(value), good: 95, warn: 90));
    }

    [Theory]
    [InlineData(0, DashboardTone.Good)]
    [InlineData(5, DashboardTone.Good)]
    [InlineData(5.1, DashboardTone.Warning)]
    [InlineData(15, DashboardTone.Warning)]
    [InlineData(15.1, DashboardTone.Critical)]
    public void A_lower_is_better_rate_is_judged_the_other_way_round(double value, DashboardTone expected)
    {
        // At-risk and failure rates: the same number that is reassuring for attendance is alarming
        // here, so the two directions cannot share one function.
        Assert.Equal(expected, DashboardFormat.InverseRateTone(Rate(value), good: 5, warn: 15));
    }

    // ---- attendance --------------------------------------------------------------------

    [Theory]
    [InlineData(20, 20, DashboardTone.Good)]
    [InlineData(19, 20, DashboardTone.Good)]
    [InlineData(92, 100, DashboardTone.Warning)]
    [InlineData(80, 100, DashboardTone.Critical)]
    public void Attendance_is_judged_on_the_proportion_present(int present, int total, DashboardTone expected)
    {
        Assert.Equal(expected, DashboardFormat.AttendanceTone(present, total));
    }

    [Fact]
    public void A_register_that_has_not_been_taken_is_not_bad_attendance()
    {
        // Zero of zero is not nought per cent. A class whose register nobody has filled in yet
        // must not be reported as a class nobody attends.
        Assert.Equal(DashboardTone.Neutral, DashboardFormat.AttendanceTone(0, 0));
        Assert.Equal("—", DashboardFormat.AttendancePercent(0, 0));
    }

    [Theory]
    [InlineData(19, 20, "95%")]
    [InlineData(1, 3, "33%")]
    public void Attendance_renders_as_a_whole_percentage(int present, int total, string expected)
    {
        Assert.Equal(expected, DashboardFormat.AttendancePercent(present, total));
    }

    // ---- averages ------------------------------------------------------------------------

    [Fact]
    public void No_marks_renders_as_absent_rather_than_zero()
    {
        // On a 1-to-5 scale a zero is not an empty state, it is worse than the worst possible
        // grade — so a student with no marks yet must not be shown one.
        Assert.Equal("—", DashboardFormat.Average([]));
    }

    [Theory]
    [InlineData(new[] { 5 }, "5")]
    [InlineData(new[] { 4, 5 }, "4.5")]
    [InlineData(new[] { 3, 4, 4 }, "3.67")]
    [InlineData(new[] { 1, 1, 1 }, "1")]
    public void An_average_keeps_at_most_two_decimals_and_drops_trailing_zeros(int[] marks, string expected)
    {
        Assert.Equal(expected, DashboardFormat.Average(marks));
    }
}
