using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics.Interfaces;

public interface ICellSuppressor
{
    Measure Apply(Measure measure);
    IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures);

    // Hides a second sibling when only one in a column is hidden, so the hidden one cannot be
    // recovered from the total by subtraction.
    IReadOnlyList<T> Complement<T>(IReadOnlyList<T> rows, Func<T, Measure> measure, Func<T, Measure, T> with);

    bool ShouldSuppress(int sampleSize);
}
