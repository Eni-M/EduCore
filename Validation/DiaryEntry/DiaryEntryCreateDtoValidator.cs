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
        // A school day has a bounded number of lesson slots; without an upper bound this
        // silently accepted values like 100 that no timetable could ever have.
        RuleFor(x => x.Period).InclusiveBetween(1, 10);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
