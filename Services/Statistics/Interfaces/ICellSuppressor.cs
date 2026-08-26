using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics.Interfaces;

public interface ICellSuppressor
{
    Measure Apply(Measure measure);
    IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures);

    // For results that are not a single Measure -- a grade distribution, for instance -- and
    // have to be withheld as a whole.
    bool ShouldSuppress(int sampleSize);
}
