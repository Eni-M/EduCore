using EduCore.Enums;

namespace EduCore.Dtos.Enrollment;

public record EnrollmentWithdrawDto(DateOnly LeftAt, EnrollmentExitReason ExitReason);
