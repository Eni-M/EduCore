using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

// Hides figures computed from too few people: with two girls in grade 12, their "average" is
// one child's own result.
//
// Averages and rates derived from a small group are suppressed. Exact headcounts are not --
// they reveal no individual's marks, and hiding a percentage whose numerator and denominator
// sit beside it would protect nothing. A count of people is fine; a measurement of them is not.
public class CellSuppressor(IOptions<PrivacyOptions> options) : ICellSuppressor
{
    private readonly int _minimum = options.Value.MinimumCellSize;

    // A sample of zero is "no data", which is not the same as a figure withheld to protect
    // someone.
    public bool ShouldSuppress(int sampleSize) => sampleSize > 0 && sampleSize < _minimum;

    public Measure Apply(Measure measure) =>
        ShouldSuppress(measure.SampleSize)
            // Baseline and Previous go too: either would leak what the value was hiding.
            ? measure with { Value = null, Baseline = null, Previous = null, Suppressed = true }
            : measure;

    public IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures) => measures.Select(Apply).ToList();
}
