using EduCore.Dtos.AuditLog;
using EduCore.Dtos.Pagination;

namespace EduCore.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResultDto<AuditLogListDto>> GetPagedAsync(
        PaginationQueryDto pagination, Guid? userId, DateTime? from, DateTime? to, CancellationToken ct);
}
