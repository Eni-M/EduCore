using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISchoolRepository : IRepository<School>
{
    Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct);
    Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct);
}
