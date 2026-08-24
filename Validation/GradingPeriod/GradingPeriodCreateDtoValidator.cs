using EduCore.Dtos.GradingPeriod;
using FluentValidation;

namespace EduCore.Validation.GradingPeriod;

public class GradingPeriodCreateDtoValidator : AbstractValidator<GradingPeriodCreateDto>
{
    public GradingPeriodCreateDtoValidator()
    {
        RuleFor(x => x.AcademicYearId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
