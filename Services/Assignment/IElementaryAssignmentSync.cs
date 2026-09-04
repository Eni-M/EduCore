namespace EduCore.Services.Assignment;

public interface IElementaryAssignmentSync
{
    // An elementary homeroom teacher teaches every subject in their class's grade curriculum.
    // Called when a class's homeroom teacher is set or changed.
    Task SyncHomeroomChangeAsync(
        Guid schoolId, Guid classId, int grade, Guid? oldTeacherId, Guid? newTeacherId, CancellationToken ct);

    // A subject was added to a grade's curriculum: give it to every elementary class of that grade
    // under its homeroom teacher (unless the class already has an active teacher for it).
    Task SyncCurriculumSubjectAddedAsync(Guid schoolId, int grade, Guid subjectId, CancellationToken ct);
}
