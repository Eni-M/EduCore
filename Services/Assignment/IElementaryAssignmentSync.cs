namespace EduCore.Services.Assignment;

// Keeps an elementary class's subject assignments in step with its homeroom teacher.
//
// Both methods only mutate tracked entities via the injected repositories -- neither calls
// SaveChangesAsync. The caller's own trailing save persists everything together, since every
// repository shares one scoped AppDbContext.
public interface IElementaryAssignmentSync
{
    Task SyncHomeroomChangeAsync(
        Guid schoolId, Guid classId, Guid? oldTeacherId, Guid? newTeacherId, CancellationToken ct);

    Task SyncNewSubjectAsync(Guid schoolId, Guid subjectId, CancellationToken ct);
}
