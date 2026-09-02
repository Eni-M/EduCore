using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IStudentMedicalRecordRepository : IRepository<StudentMedicalRecord>
{
    Task<StudentMedicalRecord?> GetByStudentAsync(Guid studentId, CancellationToken ct);
}
