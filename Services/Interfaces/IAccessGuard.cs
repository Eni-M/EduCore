namespace EduCore.Services.Interfaces;

public interface IAccessGuard
{
    Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct);

    Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct);

    Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct);
}
