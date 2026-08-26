using EduCore.Dtos.GradingPeriod;
using FluentValidation;

namespace EduCore.Validation.GradingPeriod;

public class GradingPeriodEditDtoValidator : AbstractValidator<GradingPeriodEditDto>
{
    public GradingPeriodEditDtoValidator()
    {
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
