using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics;

// Turns raw counts and averages from the repositories into Measures. Kept in one place so
// every statistic rounds the same way and carries its sample size, rather than each service
// inventing its own conventions.
public static class Measures
{
    // A percentage of a total. SampleSize is the denominator, which is what suppression and
    // the frontend both need in order to judge how much weight the figure deserves.
    public static Measure Rate(int part, int total) =>
        total == 0
            ? Measure.None()
            : Measure.Of(Math.Round(part * 100.0 / total, 2), total);

    public static Measure Average(double? value, int sampleSize) =>
        value is null
            ? Measure.None()
            : Measure.Of(Math.Round(value.Value, 2), sampleSize);

    public static Measure Count(int value) => Measure.Of(value, value);

    // Growth against an earlier figure. Undefined rather than infinite when there was nothing
    // to grow from -- going from zero students to forty is not "infinite percent growth".
    public static Measure GrowthPercent(int current, int previous) =>
        previous == 0
            ? Measure.None("no comparable earlier period")
            : Measure.Of(Math.Round((current - previous) * 100.0 / previous, 2), previous)
                .WithPrevious(previous);
}
