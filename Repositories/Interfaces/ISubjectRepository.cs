using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface ISubjectRepository : IRepository<Subject>
{
    Task<IEnumerable<Subject>> GetAllOrderedAsync(CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct);

    Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct);
}
