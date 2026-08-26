using EduCore.Dtos.Attendance;
using FluentValidation;

namespace EduCore.Validation.Attendance;

// No rule pairing IsPresent and IsExcused: Attendance.Record owns that invariant, so it holds
// however the entity is reached rather than only on this one path.
public class AttendanceEditDtoValidator : AbstractValidator<AttendanceEditDto>
{
    public AttendanceEditDtoValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleFor(x => x.BehaviorMark).InclusiveBetween(1, 5).When(x => x.BehaviorMark.HasValue);
    }
}
