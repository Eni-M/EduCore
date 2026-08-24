namespace EduCore.Models;

public class Mark
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid TeacherId { get; private set; }
    public int Value { get; private set; }
    public DateOnly Date { get; private set; }

    public Mark(Guid studentId, Guid subjectId, Guid teacherId, int value, DateOnly date)
    {
        if (value is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(value), "Mark value must be between 1 and 5.");

        Id = Guid.NewGuid();
        StudentId = studentId;
        SubjectId = subjectId;
        TeacherId = teacherId;
        Value = value;
        Date = date;
    }
}
