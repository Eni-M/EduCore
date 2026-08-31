namespace EduCore.Dtos.Statistics;

public record Measure(
    double? Value,
    double? Baseline,
    string? BaselineLabel,
    double? Previous,
    int SampleSize,
    bool Suppressed)
{
    public static Measure None(string? baselineLabel = null) =>
        new(null, null, baselineLabel, null, 0, false);

    public static Measure Of(double? value, int sampleSize) =>
        new(value, null, null, null, sampleSize, false);

    public Measure WithBaseline(double? baseline, string label) =>
        this with { Baseline = baseline, BaselineLabel = label };

    public Measure WithPrevious(double? previous) =>
        this with { Previous = previous };
}
