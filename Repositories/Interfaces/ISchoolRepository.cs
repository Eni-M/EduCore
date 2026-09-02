using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISchoolRepository : IRepository<School>
{
    Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct);
    Task<IEnumerable<School>> GetByCityAsync(Guid cityId, bool activeOnly, CancellationToken ct);
    Task<IEnumerable<School>> GetByStateAsync(Guid stateId, bool activeOnly, CancellationToken ct);
    Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct);
}
