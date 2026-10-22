using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

public class CellSuppressor(IOptions<PrivacyOptions> options) : ICellSuppressor
{
    private readonly int _minimum = options.Value.MinimumCellSize;

    public bool ShouldSuppress(int sampleSize) => sampleSize > 0 && sampleSize < _minimum;

    // The sample size goes too: "withheld, from 3 records" says what the withheld value was
    // computed from, which is half of what suppression is for.
    public Measure Apply(Measure measure) =>
        ShouldSuppress(measure.SampleSize) ? Hidden(measure) : measure;

    public IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures) => measures.Select(Apply).ToList();

    // Complementary suppression, column by column. A single hidden cell among published siblings
    // can be recovered by subtracting them from the published total (PTAC: simple suppression
    // "may not be sufficient"), so when exactly one sibling is hidden, the smallest published one
    // is hidden with it.
    public IReadOnlyList<T> Complement<T>(
        IReadOnlyList<T> rows, Func<T, Measure> measure, Func<T, Measure, T> with)
    {
        if (rows.Count(r => measure(r).Suppressed) != 1)
            return rows;

        var partner = rows
            .Where(r => !measure(r).Suppressed && measure(r).Value is not null)
            .OrderBy(r => measure(r).SampleSize)
            .FirstOrDefault();

        return partner is null
            ? rows
            : rows.Select(r => ReferenceEquals(r, partner) ? with(r, Hidden(measure(r))) : r).ToList();
    }

    private static Measure Hidden(Measure measure) =>
        measure with { Value = null, Baseline = null, Previous = null, SampleSize = 0, Suppressed = true };
}
