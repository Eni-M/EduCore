using EduCore.Dtos.Attendance;
using FluentValidation;

namespace EduCore.Validation.Attendance;

public class AttendanceCreateDtoValidator : AbstractValidator<AttendanceCreateDto>
{
    public AttendanceCreateDtoValidator()
    {
        RuleFor(x => x.DiaryEntryId).NotEmpty();
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleFor(x => x.BehaviorMark).InclusiveBetween(1, 5).When(x => x.BehaviorMark.HasValue);
    }
}
