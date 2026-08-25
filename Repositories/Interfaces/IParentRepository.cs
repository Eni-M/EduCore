using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IParentRepository : IRepository<Parent>
{
    Task<IEnumerable<Parent>> GetByStudentAsync(Guid studentId, CancellationToken ct);
}
