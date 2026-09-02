using EduCore.Dtos.SchoolingHistory;
using FluentValidation;

namespace EduCore.Validation.SchoolingHistory;

public class SchoolingHistoryCreateDtoValidator : AbstractValidator<SchoolingHistoryCreateDto>
{
    public SchoolingHistoryCreateDtoValidator()
    {
        RuleFor(x => x.PreviousSchoolName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(150);
        RuleFor(x => x.GradeOrLevel).MaximumLength(50);
        RuleFor(x => x.ExitReason).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("A schooling record cannot start in the future.");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.ToDate is not null)
            .WithMessage("The end date cannot be before the start date.");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.ToDate is not null)
            .WithMessage("A schooling record cannot end in the future.");
    }
}
