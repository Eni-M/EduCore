using EduCore.Dtos.ClassSubjectTeacher;
using FluentValidation;

namespace EduCore.Validation.ClassSubjectTeacher;

public class ClassSubjectTeacherEndDtoValidator : AbstractValidator<ClassSubjectTeacherEndDto>
{
    public ClassSubjectTeacherEndDtoValidator()
    {
        RuleFor(x => x.EndedOn).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
