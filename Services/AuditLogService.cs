using EduCore.Dtos.AuditLog;
using EduCore.Dtos.Pagination;
using EduCore.Repositories.Interfaces;
using EduCore.Configuration;
using EduCore.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services;

public class AuditLogService(
    IAuditLogRepository auditLogs,
    IOptions<PaginationOptions> pagination) : IAuditLogService
{
    public async Task<PagedResultDto<AuditLogListDto>> GetPagedAsync(
        PaginationQueryDto paging,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var page = Math.Max(1, paging.Page);

        var pageSize = Math.Clamp(paging.PageSize, 1, pagination.Value.AuditLogMaxPageSize);

        var (items, totalCount) = await auditLogs.GetPagedAsync(page, pageSize, userId, Utc(from), Utc(to), ct);

        return new PagedResultDto<AuditLogListDto>(items, page, pageSize, totalCount);
    }

    private static DateTime? Utc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
}
