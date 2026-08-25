using EduCore.Dtos.Enrollment;
using FluentValidation;

namespace EduCore.Validation.Enrollment;

public class EnrollmentCreateDtoValidator : AbstractValidator<EnrollmentCreateDto>
{
    public EnrollmentCreateDtoValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
    }
}
