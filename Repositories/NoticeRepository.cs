using EduCore.Data;
using EduCore.Dtos.Notice;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class NoticeRepository(AppDbContext context)
    : Repository<Notice>(context), INoticeRepository
{
    public async Task<IEnumerable<NoticeListDto>> GetForSchoolAsync(CancellationToken ct)
    {
        var query =
            from n in _context.Notices.AsNoTracking()
            orderby n.Pinned descending, n.CreatedAt descending
            select new NoticeListDto(
                n.Id,
                n.Title,
                n.Body,
                n.Pinned,
                _context.Users
                    .Where(u => u.Id == n.AuthorUserId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? "—",
                n.CreatedAt,
                n.UpdatedAt);

        return await query.ToListAsync(ct);
    }
}
