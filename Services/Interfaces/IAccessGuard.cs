namespace EduCore.Services.Interfaces;

public interface IAccessGuard
{
    Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct);

    Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct);

    Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct);

    // Stricter than EnsureCanViewStudentAsync: medical detail is sensitive, so a teacher is
    // allowed only as the student's homeroom teacher, not merely by teaching their class.
    Task EnsureCanViewStudentMedicalAsync(Guid studentId, CancellationToken ct);
}
