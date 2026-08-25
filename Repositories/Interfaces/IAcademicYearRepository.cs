using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAcademicYearRepository : IRepository<AcademicYear>
{
    Task<IEnumerable<AcademicYear>> GetAllOrderedAsync(CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    Task<bool> OverlapsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);
}
