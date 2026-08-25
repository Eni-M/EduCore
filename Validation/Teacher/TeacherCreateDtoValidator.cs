using EduCore.Dtos.Teacher;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.Teacher;

public class TeacherCreateDtoValidator : AbstractValidator<TeacherCreateDto>
{
    public TeacherCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).Password();
    }
}
