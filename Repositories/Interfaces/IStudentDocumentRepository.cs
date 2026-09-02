using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IStudentDocumentRepository : IRepository<StudentDocument>
{
    Task<IEnumerable<StudentDocument>> GetByStudentAsync(Guid studentId, CancellationToken ct);
}
