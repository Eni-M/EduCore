using EduCore.Dtos.DiaryEntry;
using FluentValidation;

namespace EduCore.Validation.DiaryEntry;

public class DiaryEntryCreateDtoValidator : AbstractValidator<DiaryEntryCreateDto>
{
    public DiaryEntryCreateDtoValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.Period).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
