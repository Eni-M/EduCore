using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics;

public static class Measures
{
    public static Measure Rate(int part, int total) =>
        total == 0
            ? Measure.None()
            : Measure.Of(Math.Round(part * 100.0 / total, 2), total);

    public static Measure Average(double? value, int sampleSize) =>
        value is null
            ? Measure.None()
            : Measure.Of(Math.Round(value.Value, 2), sampleSize);

    public static Measure Count(int value) => Measure.Of(value, value);

    public static Measure GrowthPercent(int current, int previous) =>
        previous == 0
            ? Measure.None("no comparable earlier period")
            : Measure.Of(Math.Round((current - previous) * 100.0 / previous, 2), previous)
                .WithPrevious(previous);
}
