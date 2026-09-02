namespace EduCore.Enums;

// Who a calendar event is for. It decides whose personal calendar the event turns up on:
// School -> everyone in the school, Class -> that class (and its guardians/teachers),
// Student -> that one student (and their guardians).
public enum CalendarAudience
{
    School, Class, Student
}
