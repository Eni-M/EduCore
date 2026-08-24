namespace EduCore.Dtos.Enrollment;

// Ends an active enrollment (transfer, withdrawal) without deleting it, so the student can
// re-enroll in the same class later (e.g. after a mid-year transfer back).
public record EnrollmentWithdrawDto(DateOnly LeftAt);
