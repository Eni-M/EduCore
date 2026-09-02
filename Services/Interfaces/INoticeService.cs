using EduCore.Dtos.Notice;

namespace EduCore.Services.Interfaces;

public interface INoticeService
{
    Task<IEnumerable<NoticeListDto>> GetForSchoolAsync(CancellationToken ct);
    Task<NoticeListDto> CreateAsync(NoticeCreateDto dto, CancellationToken ct);
    Task<NoticeListDto> UpdateAsync(Guid noticeId, NoticeEditDto dto, CancellationToken ct);
    Task DeleteAsync(Guid noticeId, CancellationToken ct);
}
