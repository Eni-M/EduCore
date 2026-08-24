using EduCore.Dtos.AcademicYear;
using FluentValidation;

namespace EduCore.Validation.AcademicYear;

public class AcademicYearCreateDtoValidator : AbstractValidator<AcademicYearCreateDto>
{
    public AcademicYearCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
