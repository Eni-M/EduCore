using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IGuardianRepository : IRepository<Guardian>
{
    Task<Guardian?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Guardian?> GetWithChildrenAsync(Guid guardianId, CancellationToken ct);
    Task<Guardian?> GetTrackedWithChildrenAsync(Guid guardianId, CancellationToken ct);
    Task<(IReadOnlyList<Guardian> Items, int TotalCount)> GetPagedBySchoolAsync(
        Guid schoolId, EduCore.Dtos.Pagination.PaginationQueryDto pagination, string? search, CancellationToken ct);
    Task<IReadOnlyList<Guardian>> SearchAsync(string query, int take, CancellationToken ct);
    Task<Guardian?> GetGloballyAsync(Guid guardianId, CancellationToken ct);
    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct);
    Task<IReadOnlyList<GuardianStudent>> GetContactsForStudentAsync(Guid studentId, CancellationToken ct);
    Task<bool> LinkExistsAsync(Guid guardianId, Guid studentId, CancellationToken ct);
    Task<GuardianStudent?> GetLinkAsync(Guid guardianId, Guid studentId, CancellationToken ct);
    Task<bool> HasChildInClassAsync(Guid guardianId, Guid classId, CancellationToken ct);

    Task AddLinkAsync(GuardianStudent link);
    void RemoveLink(GuardianStudent link);
}
