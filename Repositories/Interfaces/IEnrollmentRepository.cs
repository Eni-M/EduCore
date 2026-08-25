using EduCore.Dtos.Enrollment;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<IEnumerable<EnrollmentListDto>> GetByClassAsync(Guid classId, bool includeLeft, CancellationToken ct);
    Task<IEnumerable<EnrollmentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct);

    Task<IEnumerable<Enrollment>> GetActiveByClassAsync(Guid classId, CancellationToken ct);

    Task<Enrollment?> GetActiveForStudentAsync(Guid studentId, CancellationToken ct);

    Task<EnrollmentListDto?> GetDtoAsync(Guid enrollmentId, CancellationToken ct);
}
