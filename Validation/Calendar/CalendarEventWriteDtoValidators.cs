using EduCore.Dtos.Calendar;
using EduCore.Enums;
using FluentValidation;

namespace EduCore.Validation.Calendar;

// Assignment is a real thing a teacher creates on the assignments screen, with a due date, a
// brief and somewhere for students to hand work in. As a calendar type it was none of that --
// just a label anyone allowed to post an event could pick, the directorate included. The enum
// value stays so events already carrying it still read correctly; it can no longer be chosen.

public class CalendarEventCreateDtoValidator : AbstractValidator<CalendarEventCreateDto>
{
    public CalendarEventCreateDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Audience).IsInEnum();

        RuleFor(x => x.Type)
            .NotEqual(CalendarEventType.Assignment)
            .WithMessage("Assignments are created on the assignments screen, not on the event board.");

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

        RuleFor(x => x.Type)
            .NotEqual(CalendarEventType.Assignment)
            .WithMessage("Assignments are created on the assignments screen, not on the event board.");

        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value)
            .When(x => x.StartTime is not null && x.EndTime is not null)
            .WithMessage("The end time cannot be before the start time.");
    }
}
