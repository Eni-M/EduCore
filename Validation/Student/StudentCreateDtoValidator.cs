using EduCore.Dtos.Student;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.Student;

public class StudentCreateDtoValidator : AbstractValidator<StudentCreateDto>
{
    public StudentCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.PersonalNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DateOfBirth).LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Gender).IsInEnum();
    }
}
