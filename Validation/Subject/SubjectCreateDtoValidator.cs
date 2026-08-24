using EduCore.Dtos.Subject;
using FluentValidation;

namespace EduCore.Validation.Subject;

public class SubjectCreateDtoValidator : AbstractValidator<SubjectCreateDto>
{
    public SubjectCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
