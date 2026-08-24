using EduCore.Dtos.Teacher;
using FluentValidation;

namespace EduCore.Validation.Teacher;

public class TeacherEditDtoValidator : AbstractValidator<TeacherEditDto>
{
    public TeacherEditDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
