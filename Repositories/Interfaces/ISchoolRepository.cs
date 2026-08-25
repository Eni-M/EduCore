using EduCore.Dtos.School;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISchoolRepository : IRepository<School>
{
    Task<IEnumerable<School>> GetAllOrderedAsync(CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct);

    Task<IEnumerable<SchoolStatisticsDto>> GetStatisticsAsync(CancellationToken ct);
}
