using EduCore.Dtos.Mark;
using FluentValidation;

namespace EduCore.Validation.Mark;

public class MarkUpsertDtoValidator : AbstractValidator<MarkUpsertDto>
{
    public MarkUpsertDtoValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.GradingPeriodId).NotEmpty();

        // Either may be absent while the period is still running, but a supplied mark has to
        // be on the 1-5 scale.
        RuleFor(x => x.FirstMark).InclusiveBetween(1, 5).When(x => x.FirstMark.HasValue);
        RuleFor(x => x.SecondMark).InclusiveBetween(1, 5).When(x => x.SecondMark.HasValue);

        RuleFor(x => x)
            .Must(x => x.FirstMark.HasValue || x.SecondMark.HasValue)
            .WithMessage("Provide at least one of the two marks.");
    }
}
