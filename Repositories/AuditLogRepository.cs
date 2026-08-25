using EduCore.Data;
using EduCore.Dtos.AuditLog;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class AuditLogRepository(AppDbContext context) : Repository<AuditLog>(context), IAuditLogRepository
{
    public async Task<(IReadOnlyList<AuditLogListDto> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (userId is not null)
            query = query.Where(a => a.UserId == userId);

        if (from is not null)
            query = query.Where(a => a.Timestamp >= from);

        if (to is not null)
            query = query.Where(a => a.Timestamp <= to);

        var totalCount = await query.CountAsync(ct);

        var items = await (
                from a in query
                join u in _context.Users on a.UserId equals (Guid?)u.Id into userMatch
                from u in userMatch.DefaultIfEmpty()
                orderby a.Timestamp descending, a.Id
                select new AuditLogListDto(
                    a.Id,
                    a.UserId,
                    u == null ? null : u.FirstName + " " + u.LastName,
                    a.Action,
                    a.Timestamp))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
