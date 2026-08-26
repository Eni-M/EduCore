using EduCore.Dtos.School;
using FluentValidation;

namespace EduCore.Validation.School;

public class SchoolEditDtoValidator : AbstractValidator<SchoolEditDto>
{
    public SchoolEditDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.City).MaximumLength(120);
        RuleFor(x => x.Zone).MaximumLength(120);
    }
}
