using EduCore.Dtos.Schedule;
using FluentValidation;

namespace EduCore.Validation.Schedule;

public class ShiftPeriodWriteDtoValidator : AbstractValidator<ShiftPeriodWriteDto>
{
    public ShiftPeriodWriteDtoValidator()
    {
        RuleFor(x => x.PeriodNumber).GreaterThan(0);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("A period's end time must be after its start time.");
    }
}
