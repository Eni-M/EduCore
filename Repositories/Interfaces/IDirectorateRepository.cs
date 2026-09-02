using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IDirectorateRepository : IRepository<Directorate>
{
    Task<IEnumerable<Directorate>> GetAllAcrossSchoolsAsync(Guid? schoolId, CancellationToken ct);
    Task<IEnumerable<Directorate>> GetByCityAsync(Guid cityId, CancellationToken ct);
    Task<Directorate?> GetWithUserAsync(Guid directorateId, CancellationToken ct);

    Task<Directorate?> GetTrackedAsync(Guid directorateId, CancellationToken ct);

    Task<Directorate?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
