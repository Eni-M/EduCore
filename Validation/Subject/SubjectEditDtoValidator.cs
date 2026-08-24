using EduCore.Dtos.Subject;
using FluentValidation;

namespace EduCore.Validation.Subject;

public class SubjectEditDtoValidator : AbstractValidator<SubjectEditDto>
{
    public SubjectEditDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
