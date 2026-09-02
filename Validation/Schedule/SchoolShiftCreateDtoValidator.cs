using EduCore.Dtos.Schedule;
using FluentValidation;

namespace EduCore.Validation.Schedule;

public class SchoolShiftCreateDtoValidator : AbstractValidator<SchoolShiftCreateDto>
{
    public SchoolShiftCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
