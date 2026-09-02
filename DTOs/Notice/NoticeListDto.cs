namespace EduCore.Dtos.Notice;

public record NoticeListDto(
    Guid Id,
    string Title,
    string Body,
    bool Pinned,
    string AuthorName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
