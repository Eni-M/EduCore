using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

// Hides figures computed from too few people.
//
// "Grade 12, female, average 2.1" names a specific child when two girls are in grade 12. This
// runs in the aggregation layer rather than at the endpoints, so it holds no matter which
// slice of the statistics is exposed later -- retrofitting it once numbers are already public
// is not a fix, because they are already out.
public class CellSuppressor(IOptions<GradingOptions> options) : ICellSuppressor
{
    private readonly int _minimum = options.Value.MinimumCellSize;

    public Measure Apply(Measure measure) =>
        // A sample of zero is "no data", which is not the same thing as a figure withheld to
        // protect someone -- callers may want to say so differently.
        measure.SampleSize > 0 && measure.SampleSize < _minimum
            ? measure with { Value = null, Baseline = null, Previous = null, Suppressed = true }
            : measure;

    public IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures) =>
        measures.Select(Apply).ToList();
}
