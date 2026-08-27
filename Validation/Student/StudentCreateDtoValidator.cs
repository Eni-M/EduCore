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

        // Both optional, but a name without a number is a guardian nobody can ring, and a
        // number without a name is a number nobody can place -- so they travel together.
        RuleFor(x => x.GuardianName).NotEmpty().MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.GuardianPhone));
        RuleFor(x => x.GuardianPhone).NotEmpty().MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.GuardianName));
    }
}
