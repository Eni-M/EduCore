using EduCore.Enums;

namespace EduCore.Dtos.Calendar;

// CreatedByUserId travels so the client can tell whether the current user owns the event and may
// edit it, without a second call. ClassName/StudentName are null unless the audience names one.
public record CalendarEventListDto(
    Guid Id,
    string Title,
    string? Description,
    CalendarEventType Type,
    CalendarAudience Audience,
    Guid? ClassId,
    string? ClassName,
    Guid? StudentId,
    string? StudentName,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    Guid CreatedByUserId,
    string AuthorName);
