using EduCore.Enums;

namespace EduCore.Dtos.Calendar;

// ClassId is required only for a Class event and StudentId only for a Student event; the service
// enforces that, and clears the one that does not apply. SchoolId and the creator come from the
// caller, never the body.
public record CalendarEventCreateDto(
    string Title,
    string? Description,
    CalendarEventType Type,
    CalendarAudience Audience,
    Guid? ClassId,
    Guid? StudentId,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime);
