using EduCore.Dtos.Notice;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class NoticeService(
    INoticeRepository notices,
    ICurrentUser currentUser) : INoticeService
{
    public Task<IEnumerable<NoticeListDto>> GetForSchoolAsync(CancellationToken ct) =>
        notices.GetForSchoolAsync(ct);

    public async Task<NoticeListDto> CreateAsync(NoticeCreateDto dto, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var notice = new Notice
        {
            SchoolId = currentUser.RequireSchoolId(),
            Title = dto.Title.Trim(),
            Body = dto.Body,
            Pinned = dto.Pinned,
            AuthorUserId = currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await notices.AddAsync(notice);
        await notices.SaveChangesAsync(ct);

        return await SingleAsync(notice.Id, ct);
    }

    public async Task<NoticeListDto> UpdateAsync(Guid noticeId, NoticeEditDto dto, CancellationToken ct)
    {
        var notice = await notices.GetByIdAsync(noticeId, ct) ?? throw new NotFoundException("Notice", noticeId);

        notice.Title = dto.Title.Trim();
        notice.Body = dto.Body;
        notice.Pinned = dto.Pinned;
        notice.UpdatedAt = DateTime.UtcNow;

        notices.Update(notice);
        await notices.SaveChangesAsync(ct);

        return await SingleAsync(noticeId, ct);
    }

    public async Task DeleteAsync(Guid noticeId, CancellationToken ct)
    {
        var notice = await notices.GetByIdAsync(noticeId, ct) ?? throw new NotFoundException("Notice", noticeId);

        notices.Remove(notice);
        await notices.SaveChangesAsync(ct);
    }

    // The list projection is the only place a notice's author name is resolved, so a single write
    // reads back through it rather than mapping the entity by hand.
    private async Task<NoticeListDto> SingleAsync(Guid noticeId, CancellationToken ct) =>
        (await notices.GetForSchoolAsync(ct)).FirstOrDefault(n => n.Id == noticeId)
        ?? throw new NotFoundException("Notice", noticeId);
}
