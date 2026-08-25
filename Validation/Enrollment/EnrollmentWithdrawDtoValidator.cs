using EduCore.Dtos.Enrollment;
using FluentValidation;

namespace EduCore.Validation.Enrollment;

public class EnrollmentWithdrawDtoValidator : AbstractValidator<EnrollmentWithdrawDto>
{
    public EnrollmentWithdrawDtoValidator()
    {
        RuleFor(x => x.LeftAt).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
