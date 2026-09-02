using EduCore.Dtos.Calendar;
using FluentValidation;

namespace EduCore.Validation.Calendar;

public class CalendarEventCreateDtoValidator : AbstractValidator<CalendarEventCreateDto>
{
    public CalendarEventCreateDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Audience).IsInEnum();

        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value)
            .When(x => x.StartTime is not null && x.EndTime is not null)
            .WithMessage("The end time cannot be before the start time.");
    }
}

public class CalendarEventEditDtoValidator : AbstractValidator<CalendarEventEditDto>
{
    public CalendarEventEditDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value)
            .When(x => x.StartTime is not null && x.EndTime is not null)
            .WithMessage("The end time cannot be before the start time.");
    }
}
