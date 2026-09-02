using EduCore.Dtos.Schedule;
using FluentValidation;

namespace EduCore.Validation.Schedule;

public class ScheduleEntryCreateDtoValidator : AbstractValidator<ScheduleEntryCreateDto>
{
    public ScheduleEntryCreateDtoValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.ShiftPeriodId).NotEmpty();
        RuleFor(x => x.DayOfWeek)
            .Must(d => d is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
            .WithMessage("Lessons can only be scheduled Monday to Friday.");
    }
}
