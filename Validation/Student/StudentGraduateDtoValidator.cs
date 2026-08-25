using EduCore.Dtos.Student;
using FluentValidation;

namespace EduCore.Validation.Student;

public class StudentGraduateDtoValidator : AbstractValidator<StudentGraduateDto>
{
    public StudentGraduateDtoValidator()
    {
        RuleFor(x => x.GraduationDate).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
