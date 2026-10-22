namespace EduCore.Services.Interfaces;

public interface IAccessGuard
{
    Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct);

    Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct);

    Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct);

    // Stricter than EnsureCanViewStudentAsync: medical detail is sensitive, so a teacher is
    // allowed only as the student's homeroom teacher, not merely by teaching their class.
    Task EnsureCanViewStudentMedicalAsync(Guid studentId, CancellationToken ct);

    // The same rule as a question rather than a gate, for places that must decide (e.g. whether to
    // include a medical-category document in a listing) instead of throwing.
    Task<bool> CanViewStudentMedicalAsync(Guid studentId, CancellationToken ct);

    // A regional admin acts for one city, a state admin for one state; the school's office, and
    // everyone below, for none. The write-side school interceptor skips both admin tiers, so this
    // is the check that keeps them inside their own territory.
    Task EnsureCanManageSchoolAsync(Guid? schoolCityId, CancellationToken ct);

    // Who may write marks for a class-subject: the office, or the teacher assigned to it.
    void EnsureCanWriteMarks(Guid assignedTeacherId);

    // A teacher-guardian conversation is readable by its two participants and nobody else.
    void EnsureConversationParticipant(Guid guardianId, Guid teacherId);
}
