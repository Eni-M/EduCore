namespace EduCore.Models;

public class Attendance
{
    public Guid Id { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public DateTime Date { get; private set; }
    public bool IsPresent { get; private set; }

    public Attendance(Guid enrollmentId, DateTime date, bool isPresent)
    {
        Id = Guid.NewGuid();
        EnrollmentId = enrollmentId;
        Date = date;
        IsPresent = isPresent;
    }
}