using EduCore.Dtos.Notice;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface INoticeRepository : IRepository<Notice>
{
    // Pinned first, then newest. School scoping is applied by the query filter.
    Task<IEnumerable<NoticeListDto>> GetForSchoolAsync(CancellationToken ct);
}
