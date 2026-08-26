namespace EduCore.Dtos.Statistics;

// Every aggregate number in the statistics API is one of these rather than a bare double.
//
// A single number is a verdict; a number next to its baseline is context. Reporting "3.4"
// for a class says nothing useful -- "3.4 against a school average of 3.7 for the same
// subject and grade" does. SampleSize lets a caller tell a figure computed from four marks
// apart from one computed from four hundred.
//
// Value being null is NOT the same as zero and must never be rendered as one: it means the
// figure was suppressed (see CellSuppressor) or there was no data at all.
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
