using EduCore.Dtos.AuditLog;
using EduCore.Dtos.Pagination;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class AuditLogService(IAuditLogRepository auditLogs) : IAuditLogService
{
    public async Task<PagedResultDto<AuditLogListDto>> GetPagedAsync(
        PaginationQueryDto pagination,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var page = Math.Max(1, pagination.Page);

        // Capped at 20, not the 100 the other lists allow. One action writes one audit row per
        // entity it touched, so these rows arrive in bursts that share a trace id -- and a
        // burst is only readable as one action if the page is short enough to see it whole.
        var pageSize = Math.Clamp(pagination.PageSize, 1, 20);

        var (items, totalCount) = await auditLogs.GetPagedAsync(page, pageSize, userId, Utc(from), Utc(to), ct);

        return new PagedResultDto<AuditLogListDto>(items, page, pageSize, totalCount);
    }

    // Postgres timestamptz only accepts UTC, and a date from the query string arrives with
    // no kind at all.
    private static DateTime? Utc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
}
