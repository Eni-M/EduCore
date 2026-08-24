using EduCore.Dtos.Mark;
using FluentValidation;

namespace EduCore.Validation.Mark;

public class MarkCreateDtoValidator : AbstractValidator<MarkCreateDto>
{
    public MarkCreateDtoValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.GradingPeriodId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value).InclusiveBetween(1, 5);
        RuleFor(x => x.Date).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
