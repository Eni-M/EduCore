using EduCore.Dtos.DiaryEntry;
using FluentValidation;

namespace EduCore.Validation.DiaryEntry;

public class DiaryEntryEditDtoValidator : AbstractValidator<DiaryEntryEditDto>
{
    public DiaryEntryEditDtoValidator()
    {
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
