using EduCore.Data;
using EduCore.Enums;

namespace EduCore.Models;

// A dated thing on the school calendar -- an exam, an activity, a tournament. Audience decides
// whose personal calendar it lands on; ClassId is set only for a Class event and StudentId only
// for a Student event (the other stays null, which makes its composite FK optional). This is what
// lets two students see different calendars on the same day.
public class CalendarEvent : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public CalendarEventType Type { get; set; }
    public CalendarAudience Audience { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? StudentId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
