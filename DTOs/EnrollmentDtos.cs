namespace EduCore.Dtos;

public record EnrollmentListDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid ClassId,
    string ClassName,
    DateTime EnrolledAt);

// EnrolledAt is set server-side by the Enrollment constructor; nothing about an
// enrollment is editable after creation, so there's no EnrollmentEditDto.
public record EnrollmentCreateDto(Guid StudentId, Guid ClassId);
