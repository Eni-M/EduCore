using EduCore.Dtos.Statistics;

namespace EduCore.Services.Statistics.Interfaces;

public interface ICellSuppressor
{
    Measure Apply(Measure measure);
    IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures);
}
