using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISchoolingHistoryRepository : IRepository<SchoolingHistory>
{
    Task<IEnumerable<SchoolingHistory>> GetByStudentAsync(Guid studentId, CancellationToken ct);
}
