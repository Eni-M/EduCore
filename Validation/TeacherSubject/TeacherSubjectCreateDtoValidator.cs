using EduCore.Dtos.TeacherSubject;
using FluentValidation;

namespace EduCore.Validation.TeacherSubject;

public class TeacherSubjectCreateDtoValidator : AbstractValidator<TeacherSubjectCreateDto>
{
    public TeacherSubjectCreateDtoValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}
