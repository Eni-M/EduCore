using EduCore.Dtos.Class;
using FluentValidation;

namespace EduCore.Validation.Class;

public class ClassEditDtoValidator : AbstractValidator<ClassEditDto>
{
    public ClassEditDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Grade).GreaterThanOrEqualTo(1);
    }
}
