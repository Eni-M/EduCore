using EduCore.Dtos.Attendance;
using FluentValidation;

namespace EduCore.Validation.Attendance;

public class AttendanceEditDtoValidator : AbstractValidator<AttendanceEditDto>
{
    public AttendanceEditDtoValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleFor(x => x.BehaviorMark).InclusiveBetween(1, 5).When(x => x.BehaviorMark.HasValue);
    }
}
