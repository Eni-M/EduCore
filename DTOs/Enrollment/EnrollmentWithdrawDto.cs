using EduCore.Enums;

namespace EduCore.Dtos.Enrollment;

// Ends an active enrollment without deleting it, so the student can re-enroll in the same
// class later (e.g. after a mid-year transfer back). The reason is what lets statistics tell
// a transfer apart from a withdrawal.
public record EnrollmentWithdrawDto(DateOnly LeftAt, EnrollmentExitReason ExitReason);
