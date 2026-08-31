using EduCore.Dtos.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

public class CellSuppressor(IOptions<PrivacyOptions> options) : ICellSuppressor
{
    private readonly int _minimum = options.Value.MinimumCellSize;

    public bool ShouldSuppress(int sampleSize) => sampleSize > 0 && sampleSize < _minimum;

    public Measure Apply(Measure measure) =>
        ShouldSuppress(measure.SampleSize)
            ? measure with { Value = null, Baseline = null, Previous = null, Suppressed = true }
            : measure;

    public IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures) => measures.Select(Apply).ToList();
}
