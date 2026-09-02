using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IGuardianRepository : IRepository<Guardian>
{
    Task<Guardian?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Guardian?> GetWithChildrenAsync(Guid guardianId, CancellationToken ct);
    Task<Guardian?> GetTrackedWithChildrenAsync(Guid guardianId, CancellationToken ct);
    Task<IEnumerable<Guardian>> GetBySchoolAsync(Guid schoolId, CancellationToken ct);
    Task<IReadOnlyList<GuardianStudent>> GetContactsForStudentAsync(Guid studentId, CancellationToken ct);
    Task<bool> LinkExistsAsync(Guid guardianId, Guid studentId, CancellationToken ct);
    Task<bool> HasChildInClassAsync(Guid guardianId, Guid classId, CancellationToken ct);

    Task AddLinkAsync(GuardianStudent link);
    void RemoveLink(GuardianStudent link);
}
