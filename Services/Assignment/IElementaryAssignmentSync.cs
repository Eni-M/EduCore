namespace EduCore.Services.Assignment;

public interface IElementaryAssignmentSync
{
    Task SyncHomeroomChangeAsync(
        Guid schoolId, Guid classId, Guid? oldTeacherId, Guid? newTeacherId, CancellationToken ct);

    Task SyncNewSubjectAsync(Guid schoolId, Guid subjectId, CancellationToken ct);
}
