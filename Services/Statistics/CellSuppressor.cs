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
//
// What it does and does not cover, deliberately:
//
//   Suppressed -- averages and rates DERIVED from a small group. These are the disclosive
//   ones: with one student in the bucket, the "average" is that student's own value.
//
//   Not suppressed -- exact headcounts ("grade 12: 3 students"). An admin genuinely needs
//   these, they reveal no individual's marks or attendance, and hiding a percentage while
//   publishing the numerator and denominator beside it would protect nothing anyway. Where a
//   service skips this class for that reason, it says so in a comment.
//
// The line therefore is: a count of people is fine, a measurement of them is not.
public class CellSuppressor(IOptions<PrivacyOptions> options) : ICellSuppressor
{
    private readonly int _minimum = options.Value.MinimumCellSize;

    // A sample of zero is "no data", which is not the same thing as a figure withheld to
    // protect someone.
    public bool ShouldSuppress(int sampleSize) => sampleSize > 0 && sampleSize < _minimum;

    public Measure Apply(Measure measure) =>
        ShouldSuppress(measure.SampleSize)
            // Baseline and Previous go too: either would leak the same information the value
            // was hidden to protect.
            ? measure with { Value = null, Baseline = null, Previous = null, Suppressed = true }
            : measure;

    public IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures) =>
        measures.Select(Apply).ToList();
}
