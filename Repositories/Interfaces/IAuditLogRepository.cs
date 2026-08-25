using EduCore.Dtos.AuditLog;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<(IReadOnlyList<AuditLogListDto> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct);
}
