using EduCore.Dtos.DiaryEntry;
using FluentValidation;

namespace EduCore.Validation.DiaryEntry;

public class DiaryEntryCreateDtoValidator : AbstractValidator<DiaryEntryCreateDto>
{
    public DiaryEntryCreateDtoValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty().When(x => x.TeacherId.HasValue);
        RuleFor(x => x.Period).InclusiveBetween(1, 10);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
