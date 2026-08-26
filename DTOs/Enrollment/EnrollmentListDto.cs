using EduCore.Enums;

namespace EduCore.Dtos.Enrollment;

public record EnrollmentListDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    DateOnly EnrolledAt,
    DateOnly? LeftAt,
    EnrollmentExitReason? ExitReason);
