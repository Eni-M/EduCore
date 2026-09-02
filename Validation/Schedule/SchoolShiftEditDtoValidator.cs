using EduCore.Dtos.Schedule;
using FluentValidation;

namespace EduCore.Validation.Schedule;

public class SchoolShiftEditDtoValidator : AbstractValidator<SchoolShiftEditDto>
{
    public SchoolShiftEditDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
