namespace EduCore.Dtos.Enrollment;

// EnrolledAt is set server-side; nothing else about an active enrollment is editable, only
// endable (see EnrollmentWithdrawDto).
public record EnrollmentCreateDto(Guid StudentId, Guid ClassId);
