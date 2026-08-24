using EduCore.Dtos.Class;
using FluentValidation;

namespace EduCore.Validation.Class;

public class ClassCreateDtoValidator : AbstractValidator<ClassCreateDto>
{
    public ClassCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AcademicYearId).NotEmpty();
        RuleFor(x => x.Grade).GreaterThanOrEqualTo(1);
    }
}
