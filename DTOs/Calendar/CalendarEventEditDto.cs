using EduCore.Enums;

namespace EduCore.Dtos.Calendar;

// Who the event is for is fixed at creation -- moving it to a different class or student would be
// a different event. Only the descriptive fields and when it happens can be revised.
public record CalendarEventEditDto(
    string Title,
    string? Description,
    CalendarEventType Type,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime);
