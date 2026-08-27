using EduCore.Dtos.DiaryEntry;
using FluentValidation;

namespace EduCore.Validation.DiaryEntry;

public class DiaryEntryCreateDtoValidator : AbstractValidator<DiaryEntryCreateDto>
{
    public DiaryEntryCreateDtoValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        // Optional -- a teacher's own id is filled in server-side -- but an explicitly
        // supplied empty guid is a caller bug rather than a request to derive it.
        RuleFor(x => x.TeacherId).NotEmpty().When(x => x.TeacherId.HasValue);
        // A school day has a bounded number of lesson slots; without an upper bound this
        // silently accepted values like 100 that no timetable could ever have.
        RuleFor(x => x.Period).InclusiveBetween(1, 10);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
